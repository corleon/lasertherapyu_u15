namespace LTU_U15.Models.Commerce;

public sealed class PurchasedContentItem
{
    public required Guid ContentKey { get; init; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public DateTime PurchasedAtUtc { get; init; }
    public string? PaymentIntentId { get; init; }
    public decimal? PricePaid { get; init; }
}
