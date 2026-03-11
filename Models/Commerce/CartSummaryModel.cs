namespace LTU_U15.Models.Commerce;

public sealed class CartSummaryModel
{
    public IReadOnlyList<CartItemModel> Items { get; init; } = Array.Empty<CartItemModel>();
    public int Count => Items.Count;
    public decimal Subtotal => Items.Sum(x => x.Price);
    public bool IsEmpty => Count == 0;
}
