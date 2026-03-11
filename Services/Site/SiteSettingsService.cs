using LTU_U15.Models.Commerce;
using LTU_U15.Models.Site;
using LTU_U15.Services.Membership;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace LTU_U15.Services.Site;

public sealed class SiteSettingsService : ISiteSettingsService
{
    private static readonly Guid HomeContentKey = new("dcf18a51-6919-4cf8-89d1-36b94ce4d963");

    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IOptions<MembershipEmailSettings> _emailOptions;
    private readonly IOptions<StripeSettings> _stripeOptions;

    public SiteSettingsService(
        IUmbracoContextFactory umbracoContextFactory,
        IOptions<MembershipEmailSettings> emailOptions,
        IOptions<StripeSettings> stripeOptions)
    {
        _umbracoContextFactory = umbracoContextFactory;
        _emailOptions = emailOptions;
        _stripeOptions = stripeOptions;
    }

    public Task<SiteRuntimeSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var email = Clone(_emailOptions.Value);
        var stripe = Clone(_stripeOptions.Value);
        string? adminNotificationEmail = null;

        using var contextRef = _umbracoContextFactory.EnsureUmbracoContext();
        var home = contextRef.UmbracoContext.Content?.GetById(HomeContentKey);
        if (home != null)
        {
            adminNotificationEmail = ReadString(home, "adminNotificationEmail", adminNotificationEmail);

            email.From = ReadString(home, "smtpFromEmail", email.From);
            email.Host = ReadString(home, "smtpHost", email.Host);
            email.Port = ReadInt(home, "smtpPort", email.Port);
            email.Username = ReadString(home, "smtpUsername", email.Username);
            email.Password = ReadString(home, "smtpPassword", email.Password);
            email.EnableSsl = ReadBool(home, "smtpEnableSsl", email.EnableSsl);

            stripe.PublishableKey = ReadString(home, "stripePublishableKey", stripe.PublishableKey);
            stripe.SecretKey = ReadString(home, "stripeSecretKey", stripe.SecretKey);
            stripe.Currency = ReadString(home, "stripeCurrency", stripe.Currency);
        }

        return Task.FromResult(new SiteRuntimeSettings
        {
            AdminNotificationEmail = adminNotificationEmail,
            Email = email,
            Stripe = stripe
        });
    }

    private static string ReadString(Umbraco.Cms.Core.Models.PublishedContent.IPublishedContent content, string alias, string? fallback)
    {
        if (!content.HasProperty(alias))
        {
            return fallback ?? string.Empty;
        }

        var value = (content.Value<string>(alias) ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(value) ? (fallback ?? string.Empty) : value;
    }

    private static int ReadInt(Umbraco.Cms.Core.Models.PublishedContent.IPublishedContent content, string alias, int fallback)
    {
        if (!content.HasProperty(alias))
        {
            return fallback;
        }

        var raw = (content.Value<string>(alias) ?? string.Empty).Trim();
        return int.TryParse(raw, out var value) ? value : fallback;
    }

    private static bool ReadBool(Umbraco.Cms.Core.Models.PublishedContent.IPublishedContent content, string alias, bool fallback)
    {
        if (!content.HasProperty(alias))
        {
            return fallback;
        }

        var raw = (content.Value<string>(alias) ?? string.Empty).Trim();
        if (bool.TryParse(raw, out var value))
        {
            return value;
        }

        if (raw == "1")
        {
            return true;
        }

        if (raw == "0")
        {
            return false;
        }

        return fallback;
    }

    private static MembershipEmailSettings Clone(MembershipEmailSettings source) => new()
    {
        From = source.From,
        Host = source.Host,
        Port = source.Port,
        Username = source.Username,
        Password = source.Password,
        EnableSsl = source.EnableSsl
    };

    private static StripeSettings Clone(StripeSettings source) => new()
    {
        PublishableKey = source.PublishableKey,
        SecretKey = source.SecretKey,
        Currency = source.Currency,
        DefaultWebinarPrice = source.DefaultWebinarPrice,
        DefaultVideoPrice = source.DefaultVideoPrice,
        DefaultResearchPrice = source.DefaultResearchPrice
    };
}
