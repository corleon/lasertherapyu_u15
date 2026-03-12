using LTU_U15.Models.Commerce;
using LTU_U15.Services.Commerce;
using LTU_U15.Services.Site;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LTU_U15.Controllers;

[AllowAnonymous]
[Route("subscriptions")]
public sealed class SubscriptionsController : Controller
{
    private static readonly Guid ThreeMonthPlanKey = new("10000000-0000-0000-0000-000000000003");
    private static readonly Guid TwelveMonthPlanKey = new("10000000-0000-0000-0000-000000000012");

    private readonly IContentPurchaseService _purchaseService;
    private readonly IStripePaymentGateway _stripeGateway;
    private readonly ISiteSettingsService _siteSettingsService;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(
        IContentPurchaseService purchaseService,
        IStripePaymentGateway stripeGateway,
        ISiteSettingsService siteSettingsService,
        ILogger<SubscriptionsController> logger)
    {
        _purchaseService = purchaseService;
        _stripeGateway = stripeGateway;
        _siteSettingsService = siteSettingsService;
        _logger = logger;
    }

    [HttpPost("create-session")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSession([FromForm] string planCode, [FromForm] string? returnUrl)
    {
        var safeReturn = NormalizeReturnUrl(returnUrl, "/members/my-profile/");
        var checkoutDetails = await _purchaseService.GetCurrentMemberCheckoutDetailsAsync();
        if (checkoutDetails == null)
        {
            var loginReturn = $"/subscriptions?r={Uri.EscapeDataString(safeReturn)}";
            return Redirect($"/members/log-in/?r={Uri.EscapeDataString(loginReturn)}");
        }

        var plans = await BuildPlansAsync();
        var plan = plans.FirstOrDefault(x => x.PlanCode.Equals(planCode, StringComparison.OrdinalIgnoreCase));
        if (plan == null)
        {
            TempData["MembershipMessage"] = "Unknown subscription plan selected.";
            return Redirect($"/subscriptions?r={Uri.EscapeDataString(safeReturn)}");
        }

        try
        {
            var siteSettings = await _siteSettingsService.GetAsync();
            if (string.IsNullOrWhiteSpace(siteSettings.Stripe.SecretKey))
            {
                TempData["MembershipMessage"] = "Stripe is not configured. Set Stripe keys in Home -> Runtime Settings.";
                return Redirect($"/subscriptions?r={Uri.EscapeDataString(safeReturn)}");
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var encodedReturn = Uri.EscapeDataString(safeReturn);
            var checkoutUrl = await _stripeGateway.CreateCheckoutUrlAsync(new StripeCheckoutRequest
            {
                Currency = siteSettings.Stripe.Currency,
                SuccessUrl = $"{baseUrl}/subscriptions/success?r={encodedReturn}",
                CancelUrl = $"{baseUrl}/subscriptions?r={encodedReturn}&status=canceled",
                MemberKey = checkoutDetails.MemberKey,
                PurchaseType = StripeCheckoutPurchaseType.Subscription,
                SubscriptionPlanCode = plan.PlanCode,
                SubscriptionPlanName = plan.PlanName,
                SubscriptionDurationMonths = plan.DurationMonths,
                SubscriptionPrice = plan.Price,
                Items = new[]
                {
                    new StripeCheckoutLineItem
                    {
                        ContentKey = plan.DurationMonths == 12 ? TwelveMonthPlanKey : ThreeMonthPlanKey,
                        ProductName = plan.PlanName,
                        Price = plan.Price
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

            return Redirect(checkoutUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe subscription checkout session creation failed for plan {PlanCode}", planCode);
            TempData["MembershipMessage"] = "Payment session could not be created. Please try again.";
            return Redirect($"/subscriptions?r={Uri.EscapeDataString(safeReturn)}");
        }
    }

    private async Task<IReadOnlyList<SubscriptionPlanViewModel>> BuildPlansAsync()
    {
        var settings = (await _siteSettingsService.GetAsync()).Stripe;
        return new[]
        {
            new SubscriptionPlanViewModel
            {
                PlanCode = "med-3m",
                PlanName = "Three Month Subscription",
                DurationMonths = 3,
                Price = settings.ThreeMonthSubscriptionPrice,
                Description = "Short commitment with full access to all paid webinars, protocols, research, and videos.",
                CtaLabel = "Start 3-Month Plan"
            },
            new SubscriptionPlanViewModel
            {
                PlanCode = "med-12m",
                PlanName = "Twelve Month Subscription",
                DurationMonths = 12,
                Price = settings.TwelveMonthSubscriptionPrice,
                Description = "Best value annual access for teams and clinicians who use LTU resources every week.",
                CtaLabel = "Start 12-Month Plan"
            }
        };
    }

    private string NormalizeReturnUrl(string? returnUrl, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return returnUrl;
        }

        return fallback;
    }
}
