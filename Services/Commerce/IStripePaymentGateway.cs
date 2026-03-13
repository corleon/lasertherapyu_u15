namespace LTU_U15.Services.Commerce;

public sealed class StripeCheckoutRequest
{
    public required string Currency { get; init; }
    public required string SuccessUrl { get; init; }
    public required string CancelUrl { get; init; }
    public required Guid MemberKey { get; init; }
    public required IReadOnlyList<StripeCheckoutLineItem> Items { get; init; }
    public StripeCheckoutPurchaseType PurchaseType { get; init; } = StripeCheckoutPurchaseType.Content;
    public string? SubscriptionPlanCode { get; init; }
    public string? SubscriptionPlanName { get; init; }
    public int? SubscriptionDurationMonths { get; init; }
    public int? SubscriptionDurationMinutes { get; init; }
    public decimal? SubscriptionPrice { get; init; }
    public string? CustomerEmail { get; init; }
    public string? CustomerFirstName { get; init; }
    public string? CustomerLastName { get; init; }
    public string? CustomerPhoneNumber { get; init; }
    public string? CustomerAddress { get; init; }
    public string? CustomerAddress2 { get; init; }
    public string? CustomerCity { get; init; }
    public string? CustomerState { get; init; }
    public string? CustomerCountry { get; init; }
    public string? CustomerZipPostalCode { get; init; }
}

public sealed class StripeCheckoutLineItem
{
    public required Guid ContentKey { get; init; }
    public required string ProductName { get; init; }
    public required decimal Price { get; init; }
}

public sealed class StripeCheckoutCompletedEvent
{
    public required Guid MemberKey { get; init; }
    public required StripeCheckoutPurchaseType PurchaseType { get; init; }
    public IReadOnlyList<Guid> ContentKeys { get; init; } = Array.Empty<Guid>();
    public string? SubscriptionPlanCode { get; init; }
    public string? SubscriptionPlanName { get; init; }
    public int? SubscriptionDurationMonths { get; init; }
    public int? SubscriptionDurationMinutes { get; init; }
    public decimal? SubscriptionPrice { get; init; }
    public required string PaymentStatus { get; init; }
    public string? PaymentIntentId { get; init; }
}

public enum StripeCheckoutPurchaseType
{
    Content = 0,
    Subscription = 1
}

public interface IStripePaymentGateway
{
    Task<string> CreateCheckoutUrlAsync(StripeCheckoutRequest request, CancellationToken cancellationToken = default);
    StripeCheckoutCompletedEvent? ParseCheckoutCompletedEvent(string payload);
}
