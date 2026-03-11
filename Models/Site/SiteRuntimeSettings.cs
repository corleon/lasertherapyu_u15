using LTU_U15.Models.Commerce;
using LTU_U15.Services.Membership;

namespace LTU_U15.Models.Site;

public sealed class SiteRuntimeSettings
{
    public string? AdminNotificationEmail { get; init; }
    public required MembershipEmailSettings Email { get; init; }
    public required StripeSettings Stripe { get; init; }
}
