namespace LTU_U15.Models.Commerce;

public sealed class CheckoutViewModel
{
    public required Guid ContentKey { get; init; }
    public required string ProductName { get; init; }
    public required string ProductUrl { get; init; }
    public required string ReturnUrl { get; init; }
    public required string Currency { get; init; }
    public decimal Price { get; init; }
}
