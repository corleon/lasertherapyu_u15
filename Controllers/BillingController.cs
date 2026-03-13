using System.Text;
using LTU_U15.Models.Commerce;
using LTU_U15.Services.Commerce;
using LTU_U15.Services.Membership;
using LTU_U15.Services.Site;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LTU_U15.Controllers;

[AllowAnonymous]
[Route("billing")]
public sealed class BillingController : Controller
{
    private const string PendingCheckoutCookieName = "ltu_pending_checkout";

    private readonly IContentPurchaseService _purchaseService;
    private readonly ICartService _cartService;
    private readonly IStripePaymentGateway _stripeGateway;
    private readonly ISiteSettingsService _siteSettingsService;
    private readonly IMembershipNotificationService _membershipNotificationService;
    private readonly ILogger<BillingController> _logger;

    public BillingController(
        IContentPurchaseService purchaseService,
        ICartService cartService,
        IStripePaymentGateway stripeGateway,
        ISiteSettingsService siteSettingsService,
        IMembershipNotificationService membershipNotificationService,
        ILogger<BillingController> logger)
    {
        _purchaseService = purchaseService;
        _cartService = cartService;
        _stripeGateway = stripeGateway;
        _siteSettingsService = siteSettingsService;
        _membershipNotificationService = membershipNotificationService;
        _logger = logger;
    }

    [HttpGet("checkout")]
    public async Task<IActionResult> Checkout([FromQuery] Guid contentKey, [FromQuery] string? returnUrl)
    {
        if (contentKey == Guid.Empty)
        {
            return BadRequest("contentKey is required.");
        }

        var content = await _purchaseService.GetPayableContentAsync(contentKey);
        if (content == null)
        {
            return NotFound();
        }

        var safeReturnUrl = NormalizeReturnUrl(returnUrl, content.Url);

        if (!content.IsPaid)
        {
            return Redirect(content.Url);
        }

        var memberKey = await _purchaseService.GetCurrentMemberKeyAsync();
        if (!memberKey.HasValue)
        {
            var loginReturn = $"/billing/checkout?contentKey={contentKey}&returnUrl={Uri.EscapeDataString(safeReturnUrl)}";
            return Redirect($"/members/log-in?r={Uri.EscapeDataString(loginReturn)}");
        }

        if (await _purchaseService.CurrentMemberHasPurchasedAsync(contentKey))
        {
            return Redirect(content.Url);
        }

        var model = new CheckoutViewModel
        {
            ContentKey = content.ContentKey,
            ProductName = content.Name,
            ProductUrl = content.Url,
            ReturnUrl = safeReturnUrl,
            Currency = (await _siteSettingsService.GetAsync()).Stripe.Currency,
            Price = content.Price ?? 0m
        };

        return View("Checkout", model);
    }

    [HttpPost("create-session")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSession([FromForm] Guid contentKey, [FromForm] string? returnUrl)
    {
        if (contentKey == Guid.Empty)
        {
            return BadRequest("contentKey is required.");
        }

        var content = await _purchaseService.GetPayableContentAsync(contentKey);
        if (content == null)
        {
            return NotFound();
        }

        if (!content.IsPaid)
        {
            return Redirect(content.Url);
        }

        var checkoutDetails = await _purchaseService.GetCurrentMemberCheckoutDetailsAsync();
        if (checkoutDetails == null)
        {
            var checkoutReturn = $"/billing/checkout?contentKey={contentKey}&returnUrl={Uri.EscapeDataString(NormalizeReturnUrl(returnUrl, content.Url))}";
            return Redirect($"/members/log-in?r={Uri.EscapeDataString(checkoutReturn)}");
        }

        if (await _purchaseService.CurrentMemberHasPurchasedAsync(contentKey))
        {
            return Redirect(content.Url);
        }

        var safeReturnUrl = NormalizeReturnUrl(returnUrl, content.Url);
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var successUrl = $"{baseUrl}/billing/success?contentKey={contentKey}&returnUrl={Uri.EscapeDataString(safeReturnUrl)}";
        var cancelUrl = $"{baseUrl}/billing/cancel?contentKey={contentKey}&returnUrl={Uri.EscapeDataString(safeReturnUrl)}";

        try
        {
            var siteSettings = await _siteSettingsService.GetAsync();
            var checkoutUrl = await _stripeGateway.CreateCheckoutUrlAsync(new StripeCheckoutRequest
            {
                Currency = siteSettings.Stripe.Currency,
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                MemberKey = checkoutDetails.MemberKey,
                Items = new[]
                {
                    new StripeCheckoutLineItem
                    {
                        ContentKey = contentKey,
                        ProductName = content.Name,
                        Price = content.Price ?? 0m
                    }
                },
                CustomerEmail = checkoutDetails.Email,
                CustomerFirstName = checkoutDetails.FirstName,
                CustomerLastName = checkoutDetails.LastName,
                CustomerPhoneNumber = checkoutDetails.PhoneNumber,
                CustomerAddress = checkoutDetails.Address,
                CustomerAddress2 = checkoutDetails.Address2,
                CustomerCity = checkoutDetails.City,
                CustomerState = checkoutDetails.State,
                CustomerCountry = checkoutDetails.Country,
                CustomerZipPostalCode = checkoutDetails.ZipPostalCode
            });

            RememberPendingCheckoutItems(new[] { contentKey });
            return Redirect(checkoutUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe checkout session creation failed for content {ContentKey}", contentKey);
            TempData["MembershipMessage"] = "Payment session could not be created. Please try again.";
            return Redirect($"/billing/checkout?contentKey={contentKey}&returnUrl={Uri.EscapeDataString(safeReturnUrl)}");
        }
    }

    [HttpGet("success")]
    public async Task<IActionResult> Success([FromQuery] Guid contentKey, [FromQuery] string? returnUrl)
    {
        await ClearPendingCheckoutItemsFromCartAsync();
        ViewData["Title"] = "Payment Success";
        ViewData["ContentKey"] = contentKey;
        ViewData["ReturnUrl"] = "/members/my-profile/";
        return View();
    }

    [HttpGet("cancel")]
    public IActionResult Cancel([FromQuery] Guid contentKey, [FromQuery] string? returnUrl)
    {
        ForgetPendingCheckoutItems();
        ViewData["Title"] = "Payment Canceled";
        ViewData["ContentKey"] = contentKey;
        ViewData["ReturnUrl"] = NormalizeReturnUrl(returnUrl, "/members/my-profile/");
        return View();
    }

    [HttpPost("listen-to-stripe")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(HttpContext.Request.Body, Encoding.UTF8);
        var payload = await reader.ReadToEndAsync();

        try
        {
            var data = _stripeGateway.ParseCheckoutCompletedEvent(payload);
            if (data == null)
            {
                return Ok();
            }

            if (data.PurchaseType == StripeCheckoutPurchaseType.Subscription)
            {
                if ((!data.SubscriptionDurationMonths.HasValue && !data.SubscriptionDurationMinutes.HasValue) || !data.SubscriptionPrice.HasValue)
                {
                    _logger.LogWarning("Stripe subscription event is missing duration/price metadata for member {MemberKey}", data.MemberKey);
                    return Ok();
                }

                var activated = await _purchaseService.ActivateSubscriptionAsync(data.MemberKey, new SubscriptionActivationRequest
                {
                    PlanCode = data.SubscriptionPlanCode ?? "subscription",
                    PlanName = data.SubscriptionPlanName ?? "Subscription",
                    DurationMonths = data.SubscriptionDurationMonths ?? 0,
                    DurationMinutes = data.SubscriptionDurationMinutes,
                    Price = data.SubscriptionPrice.Value,
                    PaymentIntentId = data.PaymentIntentId
                });

                if (!activated)
                {
                    _logger.LogWarning("Could not activate subscription for member {MemberKey}", data.MemberKey);
                    return Ok();
                }

                _logger.LogInformation(
                    "Stripe subscription activated. PaymentIntentId: {PaymentIntentId}; MemberKey: {MemberKey}; Plan: {Plan}; DurationMonths: {DurationMonths}; DurationMinutes: {DurationMinutes}; Price: {Price}; PaymentStatus: {PaymentStatus}",
                    data.PaymentIntentId,
                    data.MemberKey,
                    data.SubscriptionPlanName ?? data.SubscriptionPlanCode,
                    data.SubscriptionDurationMonths,
                    data.SubscriptionDurationMinutes,
                    data.SubscriptionPrice,
                    data.PaymentStatus);

                try
                {
                    var baseUrl = $"{Request.Scheme}://{Request.Host}";
                    await _membershipNotificationService.SendSubscriptionActivatedAsync(
                        data.MemberKey,
                        data.SubscriptionPlanName ?? "Subscription",
                        BuildSubscriptionDurationLabel(data.SubscriptionDurationMonths, data.SubscriptionDurationMinutes),
                        data.SubscriptionPrice.Value,
                        data.PaymentIntentId,
                        baseUrl);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Subscription notification email sending failed for member {MemberKey}", data.MemberKey);
                }

                return Ok();
            }

            var addedContentKeys = new List<Guid>();
            foreach (var contentKey in data.ContentKeys)
            {
                var result = await _purchaseService.AddPurchaseAsync(data.MemberKey, contentKey, data.PaymentIntentId);
                if (result == PurchaseRecordResult.Failed)
                {
                    _logger.LogWarning("Could not attach purchased content {ContentKey} to member {MemberKey}", contentKey, data.MemberKey);
                    continue;
                }

                if (result == PurchaseRecordResult.Added)
                {
                    addedContentKeys.Add(contentKey);
                }
            }

            if (addedContentKeys.Count > 0)
            {
                foreach (var contentKey in addedContentKeys)
                {
                    await _cartService.RemoveAsync(contentKey);
                }

                _logger.LogInformation(
                    "Stripe purchase recorded successfully. PaymentIntentId: {PaymentIntentId}; MemberKey: {MemberKey}; ContentKeys: {ContentKeys}; PaymentStatus: {PaymentStatus}",
                    data.PaymentIntentId,
                    data.MemberKey,
                    string.Join(",", addedContentKeys),
                    data.PaymentStatus);

                try
                {
                    var baseUrl = $"{Request.Scheme}://{Request.Host}";
                    await _membershipNotificationService.SendPurchaseCompletedAsync(data.MemberKey, addedContentKeys, data.PaymentIntentId, baseUrl);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Purchase notification email sending failed for member {MemberKey}", data.MemberKey);
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe webhook processing failed.");
            return BadRequest();
        }
    }

    private string NormalizeReturnUrl(string? returnUrl, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return returnUrl;
        }

        return fallback;
    }

    private void RememberPendingCheckoutItems(IEnumerable<Guid> contentKeys)
    {
        var normalized = contentKeys
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        if (normalized.Length == 0)
        {
            ForgetPendingCheckoutItems();
            return;
        }

        Response.Cookies.Append(
            PendingCheckoutCookieName,
            string.Join("|", normalized),
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddHours(4)
            });
    }

    private async Task ClearPendingCheckoutItemsFromCartAsync()
    {
        var raw = Request.Cookies[PendingCheckoutCookieName];
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        var keys = raw
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => Guid.TryParse(x, out var value) ? value : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        foreach (var key in keys)
        {
            await _cartService.RemoveAsync(key);
        }

        ForgetPendingCheckoutItems();
    }

    private void ForgetPendingCheckoutItems()
    {
        Response.Cookies.Delete(PendingCheckoutCookieName);
    }

    private static string BuildSubscriptionDurationLabel(int? durationMonths, int? durationMinutes)
    {
        if (durationMinutes.HasValue && durationMinutes.Value > 0)
        {
            return $"{durationMinutes.Value} minute(s)";
        }

        if (durationMonths.HasValue && durationMonths.Value > 0)
        {
            return $"{durationMonths.Value} month(s)";
        }

        return "Custom";
    }
}
