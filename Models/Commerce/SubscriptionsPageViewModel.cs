namespace LTU_U15.Models.Commerce;

public sealed class SubscriptionsPageViewModel
{
    public required IReadOnlyList<SubscriptionPlanViewModel> Plans { get; init; }
    public required bool IsMemberLoggedIn { get; init; }
    public string ReturnUrl { get; init; } = "/members/my-profile/";
    public string LoginUrl { get; init; } = "/members/log-in/";
    public string RegisterUrl { get; init; } = "/members/registration/";
    public string? Message { get; init; }
    public LTU_U15.Services.Commerce.MemberSubscriptionStatusModel? SubscriptionStatus { get; init; }
}
