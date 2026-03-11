namespace LTU_U15.Models.Commerce;

public sealed class PayableContentModel
{
    public required Guid ContentKey { get; init; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required string ContentTypeAlias { get; init; }
    public decimal? Price { get; init; }
    public bool IsPaid => Price.HasValue && Price.Value > 0;
}
