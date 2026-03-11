using LTU_U15.Models.Commerce;
using LTU_U15.Services.Commerce;
using LTU_U15.Services.Site;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LTU_U15.Controllers;

[AllowAnonymous]
[Route("cart")]
public sealed class CartController : Controller
{
    private const string PendingCheckoutCookieName = "ltu_pending_checkout";

    private readonly ICartService _cartService;
    private readonly IContentPurchaseService _purchaseService;
    private readonly IStripePaymentGateway _stripeGateway;
    private readonly ISiteSettingsService _siteSettingsService;
    private readonly ILogger<CartController> _logger;

    public CartController(
        ICartService cartService,
        IContentPurchaseService purchaseService,
        IStripePaymentGateway stripeGateway,
        ISiteSettingsService siteSettingsService,
        ILogger<CartController> logger)
    {
        _cartService = cartService;
        _purchaseService = purchaseService;
        _stripeGateway = stripeGateway;
        _siteSettingsService = siteSettingsService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var cart = await _cartService.GetCartSummaryAsync();
        var memberKey = await _purchaseService.GetCurrentMemberKeyAsync();

        var model = new CartPageViewModel
        {
            Cart = cart,
            ContinueShoppingUrl = "/",
            LoginUrl = "/members/log-in/?r=%2Fcart",
            IsMemberLoggedIn = memberKey.HasValue,
            Message = TempData["CartMessage"]?.ToString()
        };

        ViewData["Title"] = "Shopping Cart";
        return View(model);
    }

    [HttpPost("add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add([FromForm] Guid contentKey, [FromForm] string? returnUrl)
    {
        var added = await _cartService.AddAsync(contentKey);
        TempData["CartMessage"] = added
            ? "Item added to cart."
            : "This item could not be added to cart.";

        return RedirectToLocal(returnUrl, "/cart");
    }

    [HttpPost("remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove([FromForm] Guid contentKey, [FromForm] string? returnUrl)
    {
        await _cartService.RemoveAsync(contentKey);
        TempData["CartMessage"] = "Item removed from cart.";
        return RedirectToLocal(returnUrl, "/cart");
    }

    [HttpPost("clear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear()
    {
        await _cartService.ClearAsync();
        TempData["CartMessage"] = "Cart cleared.";
        return Redirect("/cart");
    }

    [HttpPost("create-session")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSession()
    {
        var cart = await _cartService.GetCartSummaryAsync();
        if (cart.IsEmpty)
        {
            TempData["CartMessage"] = "Your cart is empty.";
            return Redirect("/cart");
        }

        var checkoutDetails = await _purchaseService.GetCurrentMemberCheckoutDetailsAsync();
        if (checkoutDetails == null)
        {
            return Redirect("/members/log-in/?r=%2Fcart");
        }

        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var siteSettings = await _siteSettingsService.GetAsync();
            var checkoutUrl = await _stripeGateway.CreateCheckoutUrlAsync(new StripeCheckoutRequest
            {
                Currency = siteSettings.Stripe.Currency,
                SuccessUrl = $"{baseUrl}/billing/success?returnUrl=%2Fmembers%2Fmy-profile%2F",
                CancelUrl = $"{baseUrl}/billing/cancel?returnUrl=%2Fcart",
                MemberKey = checkoutDetails.MemberKey,
                Items = cart.Items.Select(x => new StripeCheckoutLineItem
                {
                    ContentKey = x.ContentKey,
                    ProductName = x.Name,
                    Price = x.Price
                }).ToArray(),
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

            var pendingKeys = cart.Items.Select(x => x.ContentKey).ToArray();
            if (pendingKeys.Length > 0)
            {
                Response.Cookies.Append(
                    PendingCheckoutCookieName,
                    string.Join("|", pendingKeys),
                    new CookieOptions
                    {
                        HttpOnly = true,
                        IsEssential = true,
                        SameSite = SameSiteMode.Lax,
                        Secure = Request.IsHttps,
                        Expires = DateTimeOffset.UtcNow.AddHours(4)
                    });
            }

            return Redirect(checkoutUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe checkout session creation failed for cart.");
            TempData["CartMessage"] = "Payment session could not be created. Please try again.";
            return Redirect("/cart");
        }
    }

    private IActionResult RedirectToLocal(string? returnUrl, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return Redirect(fallback);
    }
}
