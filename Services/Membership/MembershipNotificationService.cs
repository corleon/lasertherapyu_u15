using System.Net;
using LTU_U15.Models.Membership;
using LTU_U15.Services.Site;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace LTU_U15.Services.Membership;

public sealed class MembershipNotificationService : IMembershipNotificationService
{
    private const string SiteName = "Laser Therapy University";

    private readonly IMembershipEmailService _emailService;
    private readonly ISiteSettingsService _siteSettingsService;
    private readonly IMemberService _memberService;
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly ILogger<MembershipNotificationService> _logger;

    public MembershipNotificationService(
        IMembershipEmailService emailService,
        ISiteSettingsService siteSettingsService,
        IMemberService memberService,
        IUmbracoContextFactory umbracoContextFactory,
        ILogger<MembershipNotificationService> logger)
    {
        _emailService = emailService;
        _siteSettingsService = siteSettingsService;
        _memberService = memberService;
        _umbracoContextFactory = umbracoContextFactory;
        _logger = logger;
    }

    public async Task SendRegistrationCompletedAsync(RegisterMemberViewModel model, string baseUrl, CancellationToken cancellationToken = default)
    {
        var settings = await _siteSettingsService.GetAsync(cancellationToken);
        var profileUrl = ToAbsoluteUrl(baseUrl, "/members/my-profile/");
        var loginUrl = ToAbsoluteUrl(baseUrl, "/members/log-in/");

        var greetingName = string.Join(" ", new[] { model.FirstName, model.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
        if (string.IsNullOrWhiteSpace(greetingName))
        {
            greetingName = model.Username;
        }

        var userBody = WrapEmail(
            "Registration completed",
            $"<p>Hello {Html(greetingName)},</p>" +
            "<p>Your Laser Therapy University account is now active.</p>" +
            "<p>You can access your profile and purchased content from your member cabinet.</p>" +
            ActionButton(profileUrl, "Open My Profile"));

        await _emailService.SendHtmlAsync(model.Email, $"{SiteName}: registration completed", userBody);

        if (!string.IsNullOrWhiteSpace(settings.AdminNotificationEmail))
        {
            var adminBody = WrapEmail(
                "New member registration",
                "<p>A new member completed registration.</p>" +
                BuildDefinitionList(new Dictionary<string, string?>
                {
                    ["Name"] = $"{model.FirstName} {model.LastName}".Trim(),
                    ["Username"] = model.Username,
                    ["Email"] = model.Email,
                    ["Phone"] = model.PhoneNumber,
                    ["Company"] = model.CompanyName,
                    ["Customer Type"] = model.CustomerType,
                    ["User Type"] = model.UserType,
                    ["Product Owned"] = model.ProductOwned,
                    ["Country"] = model.Country
                }) +
                ActionButton(loginUrl, "Open Login Page"));

            await _emailService.SendHtmlAsync(settings.AdminNotificationEmail, $"{SiteName}: new member registration", adminBody);
        }
    }

    public async Task SendPurchaseCompletedAsync(Guid memberKey, IReadOnlyCollection<Guid> contentKeys, string? paymentIntentId, string baseUrl, CancellationToken cancellationToken = default)
    {
        if (contentKeys.Count == 0)
        {
            return;
        }

        var member = _memberService.GetByKey(memberKey);
        if (member == null || string.IsNullOrWhiteSpace(member.Email))
        {
            _logger.LogWarning("Purchase notification skipped because member {MemberKey} could not be loaded.", memberKey);
            return;
        }

        var settings = await _siteSettingsService.GetAsync(cancellationToken);
        var profileUrl = ToAbsoluteUrl(baseUrl, "/members/my-profile/");

        using var contextRef = _umbracoContextFactory.EnsureUmbracoContext();
        var products = contentKeys
            .Select(key => contextRef.UmbracoContext.Content?.GetById(key))
            .Where(x => x != null)
            .Select(x => new ProductEmailItem
            {
                Name = x!.Value<string>("title")
                    ?? x.Value<string>("headline")
                    ?? x.Value<string>("articleTitle")
                    ?? x.Name,
                Url = ToAbsoluteUrl(baseUrl, x.Url() ?? "/"),
                Price = ResolvePrice(x)
            })
            .ToList();

        var greetingName = string.Join(" ", new[]
        {
            member.GetValue<string>("firstName"),
            member.GetValue<string>("lastName")
        }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

        if (string.IsNullOrWhiteSpace(greetingName))
        {
            greetingName = member.Username;
        }

        var userBody = WrapEmail(
            "Purchase confirmed",
            $"<p>Hello {Html(greetingName)},</p>" +
            "<p>Your payment was received successfully.</p>" +
            BuildProductList(products) +
            BuildDefinitionList(new Dictionary<string, string?>
            {
                ["Payment ID"] = paymentIntentId
            }) +
            ActionButton(profileUrl, "Open My Profile"));

        await _emailService.SendHtmlAsync(member.Email, $"{SiteName}: purchase confirmed", userBody);

        if (!string.IsNullOrWhiteSpace(settings.AdminNotificationEmail))
        {
            var adminBody = WrapEmail(
                "New purchase received",
                "<p>A member completed a purchase.</p>" +
                BuildDefinitionList(new Dictionary<string, string?>
                {
                    ["Member"] = $"{member.Name} ({member.Username})",
                    ["Email"] = member.Email,
                    ["Payment ID"] = paymentIntentId
                }) +
                BuildProductList(products) +
                ActionButton(profileUrl, "Open Member Cabinet"));

            await _emailService.SendHtmlAsync(settings.AdminNotificationEmail, $"{SiteName}: purchase received", adminBody);
        }
    }

    public async Task SendSubscriptionActivatedAsync(Guid memberKey, string planName, int durationMonths, decimal price, string? paymentIntentId, string baseUrl, CancellationToken cancellationToken = default)
    {
        var member = _memberService.GetByKey(memberKey);
        if (member == null || string.IsNullOrWhiteSpace(member.Email))
        {
            _logger.LogWarning("Subscription notification skipped because member {MemberKey} could not be loaded.", memberKey);
            return;
        }

        var settings = await _siteSettingsService.GetAsync(cancellationToken);
        var profileUrl = ToAbsoluteUrl(baseUrl, "/members/my-profile/");

        var greetingName = string.Join(" ", new[]
        {
            member.GetValue<string>("firstName"),
            member.GetValue<string>("lastName")
        }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

        if (string.IsNullOrWhiteSpace(greetingName))
        {
            greetingName = member.Username;
        }

        var userBody = WrapEmail(
            "Subscription activated",
            $"<p>Hello {Html(greetingName)},</p>" +
            "<p>Your LTU subscription is active. You can now access all paid content.</p>" +
            BuildDefinitionList(new Dictionary<string, string?>
            {
                ["Plan"] = planName,
                ["Duration"] = $"{durationMonths} month(s)",
                ["Price"] = $"${price:0.00}",
                ["Payment ID"] = paymentIntentId
            }) +
            ActionButton(profileUrl, "Open My Profile"));

        await _emailService.SendHtmlAsync(member.Email, $"{SiteName}: subscription activated", userBody);

        if (!string.IsNullOrWhiteSpace(settings.AdminNotificationEmail))
        {
            var adminBody = WrapEmail(
                "New subscription activated",
                "<p>A member activated a subscription.</p>" +
                BuildDefinitionList(new Dictionary<string, string?>
                {
                    ["Member"] = $"{member.Name} ({member.Username})",
                    ["Email"] = member.Email,
                    ["Plan"] = planName,
                    ["Duration"] = $"{durationMonths} month(s)",
                    ["Price"] = $"${price:0.00}",
                    ["Payment ID"] = paymentIntentId
                }) +
                ActionButton(profileUrl, "Open Member Cabinet"));

            await _emailService.SendHtmlAsync(settings.AdminNotificationEmail, $"{SiteName}: subscription activated", adminBody);
        }
    }

    private static string WrapEmail(string title, string contentHtml)
    {
        return $@"
<!DOCTYPE html>
<html>
<body style=""margin:0;padding:0;background:#f2f4f7;font-family:Arial,sans-serif;color:#334155;"">
  <div style=""max-width:640px;margin:0 auto;padding:28px 20px;"">
    <div style=""background:linear-gradient(180deg,#0b8d3f 0%,#056b2f 100%);padding:24px 28px;border-radius:18px 18px 0 0;"">
      <div style=""font-size:13px;letter-spacing:1.2px;text-transform:uppercase;color:#d7f7e4;font-weight:700;"">{SiteName}</div>
      <h1 style=""margin:10px 0 0;font-size:28px;line-height:1.2;color:#ffffff;font-family:Georgia,serif;"">{Html(title)}</h1>
    </div>
    <div style=""background:#ffffff;padding:28px;border:1px solid #dbe2ea;border-top:none;border-radius:0 0 18px 18px;"">
      {contentHtml}
    </div>
  </div>
</body>
</html>";
    }

    private static string ActionButton(string url, string text)
    {
        return $@"<p style=""margin:24px 0 0;""><a href=""{Html(url)}"" style=""display:inline-block;background:linear-gradient(180deg,#1491cf 0%,#0b5c97 100%);color:#ffffff;text-decoration:none;font-weight:700;padding:12px 20px;border-radius:10px;text-transform:uppercase;letter-spacing:.3px;"">{Html(text)}</a></p>";
    }

    private static string BuildDefinitionList(IEnumerable<KeyValuePair<string, string?>> items)
    {
        var rows = items
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => $@"<tr><td style=""padding:6px 12px 6px 0;font-weight:700;vertical-align:top;"">{Html(x.Key)}</td><td style=""padding:6px 0;vertical-align:top;"">{Html(x.Value!)}</td></tr>");

        return $@"<table style=""margin:18px 0 0;border-collapse:collapse;"">{string.Join(string.Empty, rows)}</table>";
    }

    private static string BuildProductList(IEnumerable<ProductEmailItem> products)
    {
        var items = products.Select(product =>
        {
            var price = string.IsNullOrWhiteSpace(product.Price) ? string.Empty : $"<div style=\"color:#64748b;font-size:13px;\">Price: {Html(product.Price)}</div>";
            return $@"<li style=""margin:0 0 12px;""><a href=""{Html(product.Url)}"" style=""color:#0b5c97;font-weight:700;text-decoration:none;"">{Html(product.Name)}</a>{price}</li>";
        });

        return $@"<div style=""margin-top:18px;""><div style=""font-weight:700;margin-bottom:10px;"">Products</div><ul style=""padding-left:18px;margin:0;"">{string.Join(string.Empty, items)}</ul></div>";
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);

    private static string ToAbsoluteUrl(string baseUrl, string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return baseUrl.TrimEnd('/') + "/";
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
        {
            return absolute.ToString();
        }

        return $"{baseUrl.TrimEnd('/')}/{url.TrimStart('/')}";
    }

    private static string? ResolvePrice(Umbraco.Cms.Core.Models.PublishedContent.IPublishedContent content)
    {
        var salePrice = content.HasProperty("salePrice") ? content.Value<string>("salePrice") : null;
        if (!string.IsNullOrWhiteSpace(salePrice))
        {
            return salePrice;
        }

        return content.HasProperty("price") ? content.Value<string>("price") : null;
    }

    private sealed class ProductEmailItem
    {
        public required string Name { get; init; }
        public required string Url { get; init; }
        public string? Price { get; init; }
    }
}
