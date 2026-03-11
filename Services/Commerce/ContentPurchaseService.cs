using System.Globalization;
using System.Text.Json;
using LTU_U15.Models.Commerce;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace LTU_U15.Services.Commerce;

public sealed class ContentPurchaseService : IContentPurchaseService
{
    private const string PurchasedContentKeysAlias = "purchasedContentKeys";
    private const string PurchasedContentHistoryAlias = "purchasedContentHistory";
    private const string PurchasedContentSummaryAlias = "purchasedContentSummary";
    private const string RecentlyViewedContentHistoryAlias = "recentlyViewedContentHistory";
    private const string RecentlyViewedContentSummaryAlias = "recentlyViewedContentSummary";
    private const int MaxRecentlyViewedItems = 12;

    private readonly IMemberManager _memberManager;
    private readonly IMemberService _memberService;
    private readonly IUmbracoContextFactory _umbracoContextFactory;

    public ContentPurchaseService(
        IMemberManager memberManager,
        IMemberService memberService,
        IUmbracoContextFactory umbracoContextFactory)
    {
        _memberManager = memberManager;
        _memberService = memberService;
        _umbracoContextFactory = umbracoContextFactory;
    }

    public bool IsPaidContent(IPublishedContent content)
    {
        return GetPrice(content).HasValue;
    }

    public decimal? GetPrice(IPublishedContent content)
    {
        if (content == null)
        {
            return null;
        }

        var alias = content.ContentType.Alias;

        if (alias.Equals("protocol", StringComparison.OrdinalIgnoreCase))
        {
            var salePrice = GetDecimal(content, "salePrice");
            if (salePrice.HasValue && salePrice.Value > 0)
            {
                return salePrice;
            }

            var price = GetDecimal(content, "price");
            return price is > 0 ? price : null;
        }

        var explicitPrice = GetDecimal(content, "price");
        if (explicitPrice.HasValue && explicitPrice.Value > 0)
        {
            return explicitPrice;
        }

        return null;
    }

    public Task<PayableContentModel?> GetPayableContentAsync(Guid contentKey, CancellationToken cancellationToken = default)
    {
        using var contextRef = _umbracoContextFactory.EnsureUmbracoContext();
        var content = contextRef.UmbracoContext.Content?.GetById(contentKey);

        if (content == null)
        {
            return Task.FromResult<PayableContentModel?>(null);
        }

        var model = new PayableContentModel
        {
            ContentKey = content.Key,
            Name = ResolveTitle(content),
            Url = content.Url(),
            ContentTypeAlias = content.ContentType.Alias,
            Price = GetPrice(content)
        };

        return Task.FromResult<PayableContentModel?>(model);
    }

    public async Task<Guid?> GetCurrentMemberKeyAsync(CancellationToken cancellationToken = default)
    {
        var member = await _memberManager.GetCurrentMemberAsync();
        return member?.Key;
    }

    public async Task<MemberCheckoutDetails?> GetCurrentMemberCheckoutDetailsAsync(CancellationToken cancellationToken = default)
    {
        var memberKey = await GetCurrentMemberKeyAsync(cancellationToken);
        if (!memberKey.HasValue)
        {
            return null;
        }

        var member = _memberService.GetByKey(memberKey.Value);
        if (member == null || string.IsNullOrWhiteSpace(member.Email))
        {
            return null;
        }

        return new MemberCheckoutDetails
        {
            MemberKey = memberKey.Value,
            Email = member.Email,
            FirstName = member.GetValue<string>("firstName"),
            LastName = member.GetValue<string>("lastName"),
            PhoneNumber = member.GetValue<string>("phoneNumber"),
            Address = member.GetValue<string>("address"),
            Address2 = member.GetValue<string>("address2"),
            City = member.GetValue<string>("city"),
            State = member.GetValue<string>("state"),
            Country = member.GetValue<string>("country"),
            ZipPostalCode = member.GetValue<string>("zipPostalCode")
        };
    }

    public async Task<bool> CurrentMemberHasPurchasedAsync(Guid contentKey, CancellationToken cancellationToken = default)
    {
        var memberKey = await GetCurrentMemberKeyAsync(cancellationToken);
        if (!memberKey.HasValue)
        {
            return false;
        }

        var member = _memberService.GetByKey(memberKey.Value);
        if (member == null)
        {
            return false;
        }

        var purchased = ParsePurchasedKeys(member.GetValue<string>(PurchasedContentKeysAlias));
        return purchased.Contains(contentKey);
    }

    public async Task<HashSet<Guid>> GetCurrentMemberPurchasedKeysAsync(CancellationToken cancellationToken = default)
    {
        var memberKey = await GetCurrentMemberKeyAsync(cancellationToken);
        if (!memberKey.HasValue)
        {
            return new HashSet<Guid>();
        }

        var member = _memberService.GetByKey(memberKey.Value);
        if (member == null)
        {
            return new HashSet<Guid>();
        }

        return ParsePurchasedKeys(member.GetValue<string>(PurchasedContentKeysAlias));
    }

    public async Task<IReadOnlyList<PurchasedContentItem>> GetCurrentMemberPurchasedContentAsync(CancellationToken cancellationToken = default)
    {
        var memberKey = await GetCurrentMemberKeyAsync(cancellationToken);
        if (!memberKey.HasValue)
        {
            return Array.Empty<PurchasedContentItem>();
        }

        var member = _memberService.GetByKey(memberKey.Value);
        if (member == null)
        {
            return Array.Empty<PurchasedContentItem>();
        }

        var purchaseHistory = ParsePurchaseHistory(member.GetValue<string>(PurchasedContentHistoryAlias));
        if (purchaseHistory.Count == 0)
        {
            return Array.Empty<PurchasedContentItem>();
        }

        using var contextRef = _umbracoContextFactory.EnsureUmbracoContext();
        var list = new List<PurchasedContentItem>();

        foreach (var purchase in purchaseHistory)
        {
            var content = contextRef.UmbracoContext.Content?.GetById(purchase.ContentKey);
            if (content == null)
            {
                continue;
            }

            list.Add(new PurchasedContentItem
            {
                ContentKey = purchase.ContentKey,
                Name = purchase.ProductName ?? ResolveTitle(content),
                Url = content.Url(),
                PurchasedAtUtc = purchase.PurchasedAtUtc,
                PaymentIntentId = purchase.PaymentIntentId,
                PricePaid = purchase.PricePaid
            });
        }

        return list
            .OrderByDescending(x => x.PurchasedAtUtc)
            .ToList();
    }

    public Task<PurchaseRecordResult> AddPurchaseAsync(Guid memberKey, Guid contentKey, string? paymentIntentId, CancellationToken cancellationToken = default)
    {
        var member = _memberService.GetByKey(memberKey);
        if (member == null)
        {
            return Task.FromResult(PurchaseRecordResult.Failed);
        }

        var purchased = ParsePurchasedKeys(member.GetValue<string>(PurchasedContentKeysAlias));
        var history = ParsePurchaseHistory(member.GetValue<string>(PurchasedContentHistoryAlias));
        var existing = history.FirstOrDefault(x => x.ContentKey == contentKey);

        if (existing != null)
        {
            if (string.IsNullOrWhiteSpace(existing.PaymentIntentId) && !string.IsNullOrWhiteSpace(paymentIntentId))
            {
                existing.PaymentIntentId = paymentIntentId;
                member.SetValue(PurchasedContentHistoryAlias, SerializePurchaseHistory(history));
                member.SetValue(PurchasedContentSummaryAlias, BuildPurchaseSummary(history));
                _memberService.Save(member);
            }

            return Task.FromResult(PurchaseRecordResult.AlreadyExists);
        }

        purchased.Add(contentKey);
        using var contextRef = _umbracoContextFactory.EnsureUmbracoContext();
        var content = contextRef.UmbracoContext.Content?.GetById(contentKey);

        history.Add(new PurchasedContentRecord
        {
            ContentKey = contentKey,
            PaymentIntentId = paymentIntentId,
            PurchasedAtUtc = DateTime.UtcNow,
            ProductName = content != null ? ResolveTitle(content) : null,
            PricePaid = content != null ? GetPrice(content) : null,
            ProductUrl = content?.Url(),
            BackofficeUrl = content != null ? $"/umbraco/section/content/workspace/document/edit/{content.Key}" : null
        });

        member.SetValue(PurchasedContentKeysAlias, string.Join(",", purchased.OrderBy(x => x)));
        member.SetValue(PurchasedContentHistoryAlias, SerializePurchaseHistory(history));
        member.SetValue(PurchasedContentSummaryAlias, BuildPurchaseSummary(history));
        _memberService.Save(member);

        return Task.FromResult(PurchaseRecordResult.Added);
    }

    public async Task TrackCurrentMemberRecentlyViewedAsync(Guid contentKey, CancellationToken cancellationToken = default)
    {
        var memberKey = await GetCurrentMemberKeyAsync(cancellationToken);
        if (!memberKey.HasValue)
        {
            return;
        }

        var member = _memberService.GetByKey(memberKey.Value);
        if (member == null)
        {
            return;
        }

        using var contextRef = _umbracoContextFactory.EnsureUmbracoContext();
        var content = contextRef.UmbracoContext.Content?.GetById(contentKey);
        if (content == null || !IsTrackableContent(content))
        {
            return;
        }

        var history = ParseRecentlyViewedHistory(member.GetValue<string>(RecentlyViewedContentHistoryAlias));
        history.RemoveAll(x => x.ContentKey == contentKey);
        history.Insert(0, new RecentlyViewedRecord
        {
            ContentKey = contentKey,
            ViewedAtUtc = DateTime.UtcNow,
            ProductName = ResolveTitle(content),
            ProductUrl = content.Url(),
            SectionName = ResolveSectionName(content)
        });

        if (history.Count > MaxRecentlyViewedItems)
        {
            history = history.Take(MaxRecentlyViewedItems).ToList();
        }

        member.SetValue(RecentlyViewedContentHistoryAlias, SerializeRecentlyViewedHistory(history));
        member.SetValue(RecentlyViewedContentSummaryAlias, BuildRecentlyViewedSummary(history));
        _memberService.Save(member);
    }

    public async Task<IReadOnlyList<RecentlyViewedContentItem>> GetCurrentMemberRecentlyViewedContentAsync(CancellationToken cancellationToken = default)
    {
        var memberKey = await GetCurrentMemberKeyAsync(cancellationToken);
        if (!memberKey.HasValue)
        {
            return Array.Empty<RecentlyViewedContentItem>();
        }

        var member = _memberService.GetByKey(memberKey.Value);
        if (member == null)
        {
            return Array.Empty<RecentlyViewedContentItem>();
        }

        var history = ParseRecentlyViewedHistory(member.GetValue<string>(RecentlyViewedContentHistoryAlias));
        if (history.Count == 0)
        {
            return Array.Empty<RecentlyViewedContentItem>();
        }

        using var contextRef = _umbracoContextFactory.EnsureUmbracoContext();
        var items = new List<RecentlyViewedContentItem>();

        foreach (var viewed in history.OrderByDescending(x => x.ViewedAtUtc))
        {
            var content = contextRef.UmbracoContext.Content?.GetById(viewed.ContentKey);
            var url = content?.Url() ?? viewed.ProductUrl;
            if (string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            items.Add(new RecentlyViewedContentItem
            {
                ContentKey = viewed.ContentKey,
                Name = content != null ? ResolveTitle(content) : (viewed.ProductName ?? "Unknown"),
                Url = url,
                SectionName = content != null ? ResolveSectionName(content) : (viewed.SectionName ?? "Content"),
                ViewedAtUtc = viewed.ViewedAtUtc
            });
        }

        return items;
    }

    private static string ResolveTitle(IPublishedContent content)
    {
        return content.Value<string>("title")
               ?? content.Value<string>("headline")
               ?? content.Value<string>("articleTitle")
               ?? content.Name;
    }

    private static string ResolveSectionName(IPublishedContent content)
    {
        return content.ContentType.Alias switch
        {
            "webinar" or "veterinaryWebinar" => "Webinars",
            "video" => "Videos",
            "research" => "Research",
            "protocol" => "Protocols",
            _ => "Content"
        };
    }

    private static bool IsTrackableContent(IPublishedContent content)
        => content.ContentType.Alias is "webinar" or "veterinaryWebinar" or "video" or "research" or "protocol";

    private static decimal? GetDecimal(IPublishedContent content, string alias)
    {
        if (!content.HasProperty(alias))
        {
            return null;
        }

        var raw = (content.Value<string>(alias) ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        raw = raw.Replace("$", string.Empty, StringComparison.Ordinal).Trim();

        return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static HashSet<Guid> ParsePurchasedKeys(string? raw)
    {
        var result = new HashSet<Guid>();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return result;
        }

        foreach (var item in raw.Split(new[] { ',', ';', '\n', '\r', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Guid.TryParse(item, out var key))
            {
                result.Add(key);
            }
        }

        return result;
    }

    private static List<PurchasedContentRecord> ParsePurchaseHistory(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new List<PurchasedContentRecord>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<PurchasedContentRecord>>(raw) ?? new List<PurchasedContentRecord>();
        }
        catch
        {
            return new List<PurchasedContentRecord>();
        }
    }

    private static string SerializePurchaseHistory(List<PurchasedContentRecord> history)
    {
        return JsonSerializer.Serialize(history.OrderByDescending(x => x.PurchasedAtUtc));
    }

    private static string BuildPurchaseSummary(IEnumerable<PurchasedContentRecord> history)
    {
        return string.Join(
            Environment.NewLine + Environment.NewLine,
            history
                .OrderByDescending(x => x.PurchasedAtUtc)
                .Select(x =>
                {
                    var lines = new List<string>
                    {
                        $"Product: {x.ProductName ?? "Unknown"}",
                        $"Purchased: {x.PurchasedAtUtc:u}",
                        $"Price: {(x.PricePaid.HasValue ? $"${x.PricePaid.Value:0.00}" : "-")}",
                        $"Content Key: {x.ContentKey}",
                        $"Public Url: {x.ProductUrl ?? "-"}",
                        $"Backoffice Url: {(string.IsNullOrWhiteSpace(x.BackofficeUrl) ? "-" : x.BackofficeUrl)}",
                        $"Payment ID: {(string.IsNullOrWhiteSpace(x.PaymentIntentId) ? "-" : x.PaymentIntentId)}"
                    };

                    return string.Join(Environment.NewLine, lines);
                }));
    }

    private static List<RecentlyViewedRecord> ParseRecentlyViewedHistory(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new List<RecentlyViewedRecord>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<RecentlyViewedRecord>>(raw) ?? new List<RecentlyViewedRecord>();
        }
        catch
        {
            return new List<RecentlyViewedRecord>();
        }
    }

    private static string SerializeRecentlyViewedHistory(List<RecentlyViewedRecord> history)
    {
        return JsonSerializer.Serialize(history.OrderByDescending(x => x.ViewedAtUtc));
    }

    private static string BuildRecentlyViewedSummary(IEnumerable<RecentlyViewedRecord> history)
    {
        return string.Join(
            Environment.NewLine + Environment.NewLine,
            history
                .OrderByDescending(x => x.ViewedAtUtc)
                .Select(x => string.Join(
                    Environment.NewLine,
                    new[]
                    {
                        $"Section: {x.SectionName ?? "Content"}",
                        $"Product: {x.ProductName ?? "Unknown"}",
                        $"Viewed: {x.ViewedAtUtc:u}",
                        $"Content Key: {x.ContentKey}",
                        $"Public Url: {x.ProductUrl ?? "-"}"
                    })));
    }

    private sealed class PurchasedContentRecord
    {
        public Guid ContentKey { get; set; }
        public string? PaymentIntentId { get; set; }
        public DateTime PurchasedAtUtc { get; set; }
        public string? ProductName { get; set; }
        public decimal? PricePaid { get; set; }
        public string? ProductUrl { get; set; }
        public string? BackofficeUrl { get; set; }
    }

    private sealed class RecentlyViewedRecord
    {
        public Guid ContentKey { get; set; }
        public DateTime ViewedAtUtc { get; set; }
        public string? ProductName { get; set; }
        public string? ProductUrl { get; set; }
        public string? SectionName { get; set; }
    }
}
