namespace LTU_U15.Models.Commerce;

public sealed class StripeSettings
{
    public string PublishableKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Currency { get; set; } = "usd";
    public decimal DefaultWebinarPrice { get; set; } = 29.95m;
    public decimal DefaultVideoPrice { get; set; } = 29.95m;
    public decimal DefaultResearchPrice { get; set; } = 29.95m;
}
