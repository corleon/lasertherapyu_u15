using LTU_U15.Models.Commerce;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace LTU_U15.Services.Commerce;

public interface IContentPurchaseService
{
    bool IsPaidContent(IPublishedContent content);
    decimal? GetPrice(IPublishedContent content);
    Task<PayableContentModel?> GetPayableContentAsync(Guid contentKey, CancellationToken cancellationToken = default);
    Task<Guid?> GetCurrentMemberKeyAsync(CancellationToken cancellationToken = default);
    Task<MemberCheckoutDetails?> GetCurrentMemberCheckoutDetailsAsync(CancellationToken cancellationToken = default);
    Task<bool> CurrentMemberHasPurchasedAsync(Guid contentKey, CancellationToken cancellationToken = default);
    Task<HashSet<Guid>> GetCurrentMemberPurchasedKeysAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchasedContentItem>> GetCurrentMemberPurchasedContentAsync(CancellationToken cancellationToken = default);
    Task<PurchaseRecordResult> AddPurchaseAsync(Guid memberKey, Guid contentKey, string? paymentIntentId, CancellationToken cancellationToken = default);
    Task<bool> CurrentMemberHasActiveSubscriptionAsync(CancellationToken cancellationToken = default);
    Task<MemberSubscriptionStatusModel?> GetCurrentMemberSubscriptionStatusAsync(CancellationToken cancellationToken = default);
    Task<bool> ActivateSubscriptionAsync(Guid memberKey, SubscriptionActivationRequest request, CancellationToken cancellationToken = default);
    Task TrackCurrentMemberRecentlyViewedAsync(Guid contentKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecentlyViewedContentItem>> GetCurrentMemberRecentlyViewedContentAsync(CancellationToken cancellationToken = default);
}

public enum PurchaseRecordResult
{
    Failed = 0,
    Added = 1,
    AlreadyExists = 2
}

public sealed class MemberCheckoutDetails
{
    public required Guid MemberKey { get; init; }
    public required string Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Address { get; init; }
    public string? Address2 { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Country { get; init; }
    public string? ZipPostalCode { get; init; }
}

public sealed class SubscriptionActivationRequest
{
    public required string PlanCode { get; init; }
    public required string PlanName { get; init; }
    public required int DurationMonths { get; init; }
    public required decimal Price { get; init; }
    public string? PaymentIntentId { get; init; }
}

public sealed class MemberSubscriptionStatusModel
{
    public required bool IsActive { get; init; }
    public string? PlanCode { get; init; }
    public string? PlanName { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
    public string? LastPaymentIntentId { get; init; }
}
