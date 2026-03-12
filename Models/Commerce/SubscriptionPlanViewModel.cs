namespace LTU_U15.Models.Commerce;

public sealed class SubscriptionPlanViewModel
{
    public required string PlanCode { get; init; }
    public required string PlanName { get; init; }
    public required string Description { get; init; }
    public required int DurationMonths { get; init; }
    public required decimal Price { get; init; }
    public string CtaLabel { get; init; } = "Start Plan";
}

