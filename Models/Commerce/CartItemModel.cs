namespace LTU_U15.Models.Commerce;

public sealed class CartItemModel
{
    public required Guid ContentKey { get; init; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required string ContentTypeAlias { get; init; }
    public required decimal Price { get; init; }
}
