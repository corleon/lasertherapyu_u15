namespace LTU_U15.Models.Commerce;

public sealed class CartPageViewModel
{
    public required CartSummaryModel Cart { get; init; }
    public required string LoginUrl { get; init; }
    public required string ContinueShoppingUrl { get; init; }
    public bool IsMemberLoggedIn { get; init; }
    public string? Message { get; init; }
}
