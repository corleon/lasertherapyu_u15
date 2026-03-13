using LTU_U15.Models.Membership;

namespace LTU_U15.Services.Membership;

public interface IMembershipNotificationService
{
    Task SendRegistrationCompletedAsync(RegisterMemberViewModel model, string baseUrl, CancellationToken cancellationToken = default);
    Task SendPurchaseCompletedAsync(Guid memberKey, IReadOnlyCollection<Guid> contentKeys, string? paymentIntentId, string baseUrl, CancellationToken cancellationToken = default);
    Task SendSubscriptionActivatedAsync(Guid memberKey, string planName, string durationLabel, decimal price, string? paymentIntentId, string baseUrl, CancellationToken cancellationToken = default);
    Task SendSubscriptionExpiringSoonAsync(Guid memberKey, string planName, DateTime expiresAtUtc, int daysRemaining, CancellationToken cancellationToken = default);
}
