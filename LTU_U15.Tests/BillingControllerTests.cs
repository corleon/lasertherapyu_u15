using System.Text;
using LTU_U15.Controllers;
using LTU_U15.Models.Commerce;
using LTU_U15.Services.Commerce;
using LTU_U15.Services.Membership;
using LTU_U15.Services.Site;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;

namespace LTU_U15.Tests;

public class BillingControllerTests
{
    [Fact]
    public async Task Checkout_WhenAnonymous_RedirectsToMembersLogin()
    {
        var purchase = new Mock<IContentPurchaseService>();
        var key = Guid.NewGuid();
        purchase.Setup(x => x.GetPayableContentAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayableContentModel
            {
                ContentKey = key,
                Name = "Product A",
                Url = "/webinars/test",
                ContentTypeAlias = "webinar",
                Price = 29.95m
            });
        purchase.Setup(x => x.GetCurrentMemberKeyAsync(It.IsAny<CancellationToken>())).ReturnsAsync((Guid?)null);

        var sut = CreateController(purchaseService: purchase);

        var result = await sut.Checkout(key, "/webinars/test");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.StartsWith("/members/log-in?r=", redirect.Url, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Checkout_WhenAlreadyPurchased_RedirectsToContentUrl()
    {
        var purchase = new Mock<IContentPurchaseService>();
        var key = Guid.NewGuid();
        purchase.Setup(x => x.GetPayableContentAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayableContentModel
            {
                ContentKey = key,
                Name = "Product A",
                Url = "/research/item",
                ContentTypeAlias = "research",
                Price = 29.95m
            });
        purchase.Setup(x => x.GetCurrentMemberKeyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid());
        purchase.Setup(x => x.CurrentMemberHasPurchasedAsync(key, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = CreateController(purchaseService: purchase);

        var result = await sut.Checkout(key, "/research/item");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/research/item", redirect.Url);
    }

    [Fact]
    public async Task CreateSession_WhenValid_RedirectsToStripeCheckoutUrl()
    {
        var purchase = new Mock<IContentPurchaseService>();
        var key = Guid.NewGuid();
        var memberKey = Guid.NewGuid();
        purchase.Setup(x => x.GetPayableContentAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayableContentModel
            {
                ContentKey = key,
                Name = "Protocol A",
                Url = "/protocols/a",
                ContentTypeAlias = "protocol",
                Price = 19.95m
            });
        purchase.Setup(x => x.GetCurrentMemberKeyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(memberKey);
        purchase.Setup(x => x.GetCurrentMemberCheckoutDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemberCheckoutDetails
            {
                MemberKey = memberKey,
                Email = "buyer@example.com",
                FirstName = "Test",
                LastName = "Buyer",
                Address = "1 Main St",
                City = "Chicago",
                State = "IL",
                Country = "United States",
                ZipPostalCode = "60611"
            });
        purchase.Setup(x => x.CurrentMemberHasPurchasedAsync(key, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var stripe = new Mock<IStripePaymentGateway>();
        stripe.Setup(x => x.CreateCheckoutUrlAsync(It.IsAny<StripeCheckoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://checkout.stripe.test/session");

        var sut = CreateController(purchaseService: purchase, stripeGateway: stripe);

        var result = await sut.CreateSession(key, "/protocols/a");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://checkout.stripe.test/session", redirect.Url);
        stripe.Verify(x => x.CreateCheckoutUrlAsync(It.Is<StripeCheckoutRequest>(r =>
            r.MemberKey == memberKey &&
            r.Items.Count == 1 &&
            r.Items[0].ContentKey == key &&
            r.CustomerEmail == "buyer@example.com" &&
            r.CustomerAddress == "1 Main St"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Checkout_WhenAuthorizedAndNotPurchased_ReturnsCheckoutViewModel()
    {
        var purchase = new Mock<IContentPurchaseService>();
        var key = Guid.NewGuid();
        purchase.Setup(x => x.GetPayableContentAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayableContentModel
            {
                ContentKey = key,
                Name = "Protocol A",
                Url = "/protocols/a",
                ContentTypeAlias = "protocol",
                Price = 19.95m
            });
        purchase.Setup(x => x.GetCurrentMemberKeyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid());
        purchase.Setup(x => x.CurrentMemberHasPurchasedAsync(key, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = CreateController(purchaseService: purchase);

        var result = await sut.Checkout(key, "/protocols/a");

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Checkout", view.ViewName);
        var model = Assert.IsType<CheckoutViewModel>(view.Model);
        Assert.Equal("/protocols/a", model.ReturnUrl);
        Assert.Equal(19.95m, model.Price);
    }

    [Fact]
    public async Task Success_AlwaysPointsToMyProfile()
    {
        var sut = CreateController();

        var result = await sut.Success(Guid.NewGuid(), "/protocols/a");

        Assert.IsType<ViewResult>(result);
        Assert.Equal("/members/my-profile/", sut.ViewData["ReturnUrl"]);
    }

    [Fact]
    public async Task Success_WhenPendingCheckoutCookieExists_RemovesItemsFromCart()
    {
        var cartService = new Mock<ICartService>();
        var keyOne = Guid.NewGuid();
        var keyTwo = Guid.NewGuid();
        var sut = CreateController(cartService: cartService);
        sut.ControllerContext.HttpContext.Request.Headers.Cookie = $"ltu_pending_checkout={keyOne}|{keyTwo}";

        var result = await sut.Success(Guid.NewGuid(), "/members/my-profile/");

        Assert.IsType<ViewResult>(result);
        cartService.Verify(x => x.RemoveAsync(keyOne, It.IsAny<CancellationToken>()), Times.Once);
        cartService.Verify(x => x.RemoveAsync(keyTwo, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Webhook_WhenPaid_AddsPurchase()
    {
        var purchase = new Mock<IContentPurchaseService>();
        var memberKey = Guid.NewGuid();
        var contentKey = Guid.NewGuid();

        var stripe = new Mock<IStripePaymentGateway>();
        stripe.Setup(x => x.ParseCheckoutCompletedEvent(It.IsAny<string>()))
            .Returns(new StripeCheckoutCompletedEvent
            {
                MemberKey = memberKey,
                ContentKeys = new[] { contentKey },
                PaymentStatus = "succeeded",
                PaymentIntentId = "pi_test_123"
            });

        purchase.Setup(x => x.AddPurchaseAsync(memberKey, contentKey, "pi_test_123", It.IsAny<CancellationToken>())).ReturnsAsync(PurchaseRecordResult.Added);

        var notifications = new Mock<IMembershipNotificationService>();
        notifications.Setup(x => x.SendPurchaseCompletedAsync(memberKey, It.IsAny<IReadOnlyCollection<Guid>>(), "pi_test_123", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateController(purchaseService: purchase, stripeGateway: stripe, notificationService: notifications);

        var payload = "{\"type\":\"checkout.session.completed\"}";
        sut.ControllerContext.HttpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        var result = await sut.Webhook();

        Assert.IsType<OkResult>(result);
        purchase.Verify(x => x.AddPurchaseAsync(memberKey, contentKey, "pi_test_123", It.IsAny<CancellationToken>()), Times.Once);
        notifications.Verify(x => x.SendPurchaseCompletedAsync(
            memberKey,
            It.Is<IReadOnlyCollection<Guid>>(c => c.Count == 1 && c.Contains(contentKey)),
            "pi_test_123",
            It.Is<string>(b => b.StartsWith("https://localhost", StringComparison.OrdinalIgnoreCase)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Webhook_WhenEventIsNotPaymentIntentSucceeded_ReturnsOkWithoutLoggingOrSaving()
    {
        var purchase = new Mock<IContentPurchaseService>();
        var stripe = new Mock<IStripePaymentGateway>();
        stripe.Setup(x => x.ParseCheckoutCompletedEvent(It.IsAny<string>()))
            .Returns((StripeCheckoutCompletedEvent?)null);

        var sut = CreateController(purchaseService: purchase, stripeGateway: stripe);

        var payload = "{\"type\":\"checkout.session.completed\"}";
        sut.ControllerContext.HttpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));

        var result = await sut.Webhook();

        Assert.IsType<OkResult>(result);
        purchase.Verify(x => x.AddPurchaseAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Webhook_WhenPurchaseAlreadyExists_DoesNotSendPurchaseNotification()
    {
        var purchase = new Mock<IContentPurchaseService>();
        var memberKey = Guid.NewGuid();
        var contentKey = Guid.NewGuid();

        var stripe = new Mock<IStripePaymentGateway>();
        stripe.Setup(x => x.ParseCheckoutCompletedEvent(It.IsAny<string>()))
            .Returns(new StripeCheckoutCompletedEvent
            {
                MemberKey = memberKey,
                ContentKeys = new[] { contentKey },
                PaymentStatus = "succeeded",
                PaymentIntentId = "pi_test_123"
            });

        purchase.Setup(x => x.AddPurchaseAsync(memberKey, contentKey, "pi_test_123", It.IsAny<CancellationToken>())).ReturnsAsync(PurchaseRecordResult.AlreadyExists);

        var notifications = new Mock<IMembershipNotificationService>();
        var sut = CreateController(purchaseService: purchase, stripeGateway: stripe, notificationService: notifications);

        var payload = "{\"type\":\"payment_intent.succeeded\"}";
        sut.ControllerContext.HttpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));

        var result = await sut.Webhook();

        Assert.IsType<OkResult>(result);
        notifications.Verify(x => x.SendPurchaseCompletedAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static BillingController CreateController(
        Mock<IContentPurchaseService>? purchaseService = null,
        Mock<ICartService>? cartService = null,
        Mock<IStripePaymentGateway>? stripeGateway = null,
        Mock<ISiteSettingsService>? siteSettingsService = null,
        Mock<IMembershipNotificationService>? notificationService = null,
        Mock<ILogger<BillingController>>? logger = null)
    {
        purchaseService ??= new Mock<IContentPurchaseService>();
        cartService ??= new Mock<ICartService>();
        stripeGateway ??= new Mock<IStripePaymentGateway>();
        siteSettingsService ??= new Mock<ISiteSettingsService>();
        siteSettingsService.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new LTU_U15.Models.Site.SiteRuntimeSettings
        {
            AdminNotificationEmail = "admin@example.com",
            Email = new MembershipEmailSettings(),
            Stripe = new StripeSettings { Currency = "usd" }
        });
        notificationService ??= new Mock<IMembershipNotificationService>();
        logger ??= new Mock<ILogger<BillingController>>();

        var controller = new BillingController(
            purchaseService.Object,
            cartService.Object,
            stripeGateway.Object,
            siteSettingsService.Object,
            notificationService.Object,
            logger.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());
        controller.Url = Mock.Of<IUrlHelper>(x => x.IsLocalUrl(It.IsAny<string>()) == true);
        controller.ControllerContext.HttpContext.Request.Scheme = "https";
        controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost", 44335);

        return controller;
    }
}
