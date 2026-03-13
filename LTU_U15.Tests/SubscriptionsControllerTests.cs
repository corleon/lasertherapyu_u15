using LTU_U15.Controllers;
using LTU_U15.Models.Commerce;
using LTU_U15.Models.Site;
using LTU_U15.Services.Commerce;
using LTU_U15.Services.Membership;
using LTU_U15.Services.Site;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;

namespace LTU_U15.Tests;

public class SubscriptionsControllerTests
{
    [Fact]
    public async Task CreateSession_WhenAnonymous_RedirectsToLogin()
    {
        var purchase = new Mock<IContentPurchaseService>();
        purchase.Setup(x => x.GetCurrentMemberCheckoutDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((MemberCheckoutDetails?)null);

        var sut = CreateController(purchaseService: purchase);

        var result = await sut.CreateSession("med-3m", "/research/test");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.StartsWith("/members/log-in/?r=", redirect.Url, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateSession_WhenValidPlan_CreatesSubscriptionCheckout()
    {
        var purchase = new Mock<IContentPurchaseService>();
        var memberKey = Guid.NewGuid();
        purchase.Setup(x => x.GetCurrentMemberCheckoutDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemberCheckoutDetails
            {
                MemberKey = memberKey,
                Email = "member@example.com"
            });

        var stripe = new Mock<IStripePaymentGateway>();
        stripe.Setup(x => x.CreateCheckoutUrlAsync(It.IsAny<StripeCheckoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://checkout.stripe.test/subscription");

        var settings = new Mock<ISiteSettingsService>();
        settings.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SiteRuntimeSettings
        {
            AdminNotificationEmail = "admin@example.com",
            Email = new MembershipEmailSettings(),
            Stripe = new StripeSettings
            {
                SecretKey = "sk_test_123",
                Currency = "usd",
                ThreeMonthSubscriptionPrice = 199.00m,
                TwelveMonthSubscriptionPrice = 499.00m
            }
        });

        var sut = CreateController(purchaseService: purchase, stripeGateway: stripe, siteSettingsService: settings);

        var result = await sut.CreateSession("med-12m", "/members/my-profile/");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://checkout.stripe.test/subscription", redirect.Url);
        stripe.Verify(x => x.CreateCheckoutUrlAsync(It.Is<StripeCheckoutRequest>(r =>
            r.MemberKey == memberKey &&
            r.PurchaseType == StripeCheckoutPurchaseType.Subscription &&
            r.SubscriptionPlanCode == "med-12m" &&
            r.SubscriptionDurationMonths == 12 &&
            r.SubscriptionPrice == 499.00m &&
            r.Items.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateSession_WhenStripeSecretMissing_RedirectsBackWithMessage()
    {
        var purchase = new Mock<IContentPurchaseService>();
        purchase.Setup(x => x.GetCurrentMemberCheckoutDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemberCheckoutDetails
            {
                MemberKey = Guid.NewGuid(),
                Email = "member@example.com"
            });

        var settings = new Mock<ISiteSettingsService>();
        settings.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SiteRuntimeSettings
        {
            AdminNotificationEmail = "admin@example.com",
            Email = new MembershipEmailSettings(),
            Stripe = new StripeSettings
            {
                Currency = "usd",
                SecretKey = string.Empty,
                ThreeMonthSubscriptionPrice = 199.00m,
                TwelveMonthSubscriptionPrice = 499.00m
            }
        });

        var stripe = new Mock<IStripePaymentGateway>();
        var sut = CreateController(purchaseService: purchase, stripeGateway: stripe, siteSettingsService: settings);

        var result = await sut.CreateSession("med-3m", "/members/my-profile/");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.StartsWith("/subscriptions?r=", redirect.Url, StringComparison.Ordinal);
        Assert.Equal("Stripe is not configured. Set Stripe keys in Home -> Runtime Settings.", sut.TempData["MembershipMessage"]);
        stripe.Verify(x => x.CreateCheckoutUrlAsync(It.IsAny<StripeCheckoutRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateSession_WhenTenMinuteTestPlanEnabled_CreatesSubscriptionCheckoutWithMinutes()
    {
        var purchase = new Mock<IContentPurchaseService>();
        var memberKey = Guid.NewGuid();
        purchase.Setup(x => x.GetCurrentMemberCheckoutDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemberCheckoutDetails
            {
                MemberKey = memberKey,
                Email = "member@example.com"
            });

        var stripe = new Mock<IStripePaymentGateway>();
        stripe.Setup(x => x.CreateCheckoutUrlAsync(It.IsAny<StripeCheckoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://checkout.stripe.test/subscription-test");

        var settings = new Mock<ISiteSettingsService>();
        settings.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SiteRuntimeSettings
        {
            AdminNotificationEmail = "admin@example.com",
            Email = new MembershipEmailSettings(),
            Stripe = new StripeSettings
            {
                SecretKey = "sk_test_123",
                Currency = "usd",
                ThreeMonthSubscriptionPrice = 199.00m,
                TwelveMonthSubscriptionPrice = 499.00m,
                EnableTenMinuteTestSubscription = true,
                TenMinuteTestSubscriptionPrice = 1.00m,
                TenMinuteTestSubscriptionDurationMinutes = 10
            }
        });

        var sut = CreateController(purchaseService: purchase, stripeGateway: stripe, siteSettingsService: settings);

        var result = await sut.CreateSession("med-10m-test", "/members/my-profile/");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://checkout.stripe.test/subscription-test", redirect.Url);
        stripe.Verify(x => x.CreateCheckoutUrlAsync(It.Is<StripeCheckoutRequest>(r =>
            r.MemberKey == memberKey &&
            r.PurchaseType == StripeCheckoutPurchaseType.Subscription &&
            r.SubscriptionPlanCode == "med-10m-test" &&
            r.SubscriptionDurationMonths == 0 &&
            r.SubscriptionDurationMinutes == 10 &&
            r.SubscriptionPrice == 1.00m &&
            r.Items.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static SubscriptionsController CreateController(
        Mock<IContentPurchaseService>? purchaseService = null,
        Mock<IStripePaymentGateway>? stripeGateway = null,
        Mock<ISiteSettingsService>? siteSettingsService = null,
        Mock<ILogger<SubscriptionsController>>? logger = null)
    {
        purchaseService ??= new Mock<IContentPurchaseService>();
        stripeGateway ??= new Mock<IStripePaymentGateway>();
        if (siteSettingsService == null)
        {
            siteSettingsService = new Mock<ISiteSettingsService>();
            siteSettingsService.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SiteRuntimeSettings
            {
                AdminNotificationEmail = "admin@example.com",
                Email = new MembershipEmailSettings(),
                Stripe = new StripeSettings
                {
                    SecretKey = "sk_test_123"
                }
            });
        }
        logger ??= new Mock<ILogger<SubscriptionsController>>();

        var controller = new SubscriptionsController(
            purchaseService.Object,
            stripeGateway.Object,
            siteSettingsService.Object,
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
