namespace LTU_U15.Models.Commerce;

public sealed class RecentlyViewedContentItem
{
    public required Guid ContentKey { get; init; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required string SectionName { get; init; }
    public required DateTime ViewedAtUtc { get; init; }
}
