using LTU_U15.Models.Membership;

namespace LTU_U15.Services.Membership;

public interface IMembershipNotificationService
{
    Task SendRegistrationCompletedAsync(RegisterMemberViewModel model, string baseUrl, CancellationToken cancellationToken = default);
    Task SendPurchaseCompletedAsync(Guid memberKey, IReadOnlyCollection<Guid> contentKeys, string? paymentIntentId, string baseUrl, CancellationToken cancellationToken = default);
}
