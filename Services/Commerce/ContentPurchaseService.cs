using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using LTU_U15.Models.Commerce;
using LTU_U15.Services.Membership;
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
    private const string SubscriptionPlanAlias = "subscriptionPlan";
    private const string SubscriptionStatusAlias = "subscriptionStatus";
    private const string SubscriptionExpiresUtcAlias = "subscriptionExpiresUtc";
    private const string SubscriptionLastPaymentIntentIdAlias = "subscriptionLastPaymentIntentId";
    private const string SubscriptionHistoryAlias = "subscriptionHistory";
    private const string SubscriptionSummaryAlias = "subscriptionSummary";
    private const int MaxRecentlyViewedItems = 20;
    private const int SubscriptionExpiryWarningDays = 7;

    private readonly IMemberManager _memberManager;
    private readonly IMemberService _memberService;
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IMembershipNotificationService _membershipNotificationService;
    private readonly ILogger<ContentPurchaseService> _logger;

    public ContentPurchaseService(
        IMemberManager memberManager,
        IMemberService memberService,
        IUmbracoContextFactory umbracoContextFactory,
        IMembershipNotificationService membershipNotificationService,
        ILogger<ContentPurchaseService> logger)
    {
        _memberManager = memberManager;
        _memberService = memberService;
        _umbracoContextFactory = umbracoContextFactory;
        _membershipNotificationService = membershipNotificationService;
        _logger = logger;
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

        if (MemberHasActiveSubscription(member, DateTime.UtcNow))
        {
            return true;
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

    public async Task<bool> CurrentMemberHasActiveSubscriptionAsync(CancellationToken cancellationToken = default)
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

        var state = await EnsureMemberSubscriptionStateAsync(member, DateTime.UtcNow, sendExpiryWarningEmail: true, cancellationToken);
        return state.IsActive;
    }

    public async Task<MemberSubscriptionStatusModel?> GetCurrentMemberSubscriptionStatusAsync(CancellationToken cancellationToken = default)
    {
        var memberKey = await GetCurrentMemberKeyAsync(cancellationToken);
        if (!memberKey.HasValue)
        {
            return null;
        }

        var member = _memberService.GetByKey(memberKey.Value);
        if (member == null)
        {
            return null;
        }

        var state = await EnsureMemberSubscriptionStateAsync(member, DateTime.UtcNow, sendExpiryWarningEmail: true, cancellationToken);

        return new MemberSubscriptionStatusModel
        {
            IsActive = state.IsActive,
            IsExpiringSoon = state.IsExpiringSoon,
            PlanCode = state.PlanCode,
            PlanName = state.PlanName,
            ExpiresAtUtc = state.ExpiresAtUtc,
            DaysRemaining = state.DaysRemaining,
            RemainingLabel = state.RemainingLabel,
            LastPaymentIntentId = state.LastPaymentIntentId
        };
    }

    public async Task<IReadOnlyList<MemberSubscriptionHistoryItem>> GetCurrentMemberSubscriptionHistoryAsync(CancellationToken cancellationToken = default)
    {
        var memberKey = await GetCurrentMemberKeyAsync(cancellationToken);
        if (!memberKey.HasValue)
        {
            return Array.Empty<MemberSubscriptionHistoryItem>();
        }

        var member = _memberService.GetByKey(memberKey.Value);
        if (member == null)
        {
            return Array.Empty<MemberSubscriptionHistoryItem>();
        }

        await EnsureMemberSubscriptionStateAsync(member, DateTime.UtcNow, sendExpiryWarningEmail: true, cancellationToken);

        var utcNow = DateTime.UtcNow;
        var history = ParseSubscriptionHistory(GetMemberValue(member, SubscriptionHistoryAlias))
            .OrderByDescending(x => x.ActivatedAtUtc)
            .ThenByDescending(x => x.ExpiresAtUtc)
            .ToList();

        if (history.Count == 0)
        {
            return Array.Empty<MemberSubscriptionHistoryItem>();
        }

        var currentRecord = history
            .OrderByDescending(x => x.ExpiresAtUtc)
            .ThenByDescending(x => x.ActivatedAtUtc)
            .FirstOrDefault();

        return history
            .Select(record => new MemberSubscriptionHistoryItem
            {
                PlanCode = record.PlanCode,
                PlanName = record.PlanName,
                DurationMonths = record.DurationMonths,
                DurationMinutes = record.DurationMinutes,
                Price = record.Price,
                ActivatedAtUtc = record.ActivatedAtUtc,
                ExpiresAtUtc = record.ExpiresAtUtc,
                PaymentIntentId = record.PaymentIntentId,
                IsActive = record.ExpiresAtUtc > utcNow,
                IsCurrent = currentRecord != null && record.ActivatedAtUtc == currentRecord.ActivatedAtUtc && record.ExpiresAtUtc == currentRecord.ExpiresAtUtc && string.Equals(record.PaymentIntentId, currentRecord.PaymentIntentId, StringComparison.Ordinal)
            })
            .ToList();
    }

    public Task<bool> ActivateSubscriptionAsync(Guid memberKey, SubscriptionActivationRequest request, CancellationToken cancellationToken = default)
    {
        var hasMonthsDuration = request.DurationMonths > 0;
        var hasMinutesDuration = request.DurationMinutes.HasValue && request.DurationMinutes.Value > 0;
        if (!hasMonthsDuration && !hasMinutesDuration)
        {
            return Task.FromResult(false);
        }

        var member = _memberService.GetByKey(memberKey);
        if (member == null)
        {
            return Task.FromResult(false);
        }

        var utcNow = DateTime.UtcNow;
        var currentExpiry = ParseUtc(GetMemberValue(member, SubscriptionExpiresUtcAlias));
        var baseDate = currentExpiry.HasValue && currentExpiry.Value > utcNow ? currentExpiry.Value : utcNow;
        var nextExpiry = hasMinutesDuration
            ? baseDate.AddMinutes(request.DurationMinutes!.Value)
            : baseDate.AddMonths(request.DurationMonths);

        var history = ParseSubscriptionHistory(GetMemberValue(member, SubscriptionHistoryAlias));
        history.Add(new SubscriptionRecord
        {
            PlanCode = request.PlanCode,
            PlanName = request.PlanName,
            DurationMonths = request.DurationMonths,
            DurationMinutes = request.DurationMinutes,
            Price = request.Price,
            PaymentIntentId = request.PaymentIntentId,
            ActivatedAtUtc = utcNow,
            ExpiresAtUtc = nextExpiry,
            ExpiryWarningSentAtUtc = null
        });

        SetMemberValue(member, SubscriptionPlanAlias, request.PlanName);
        SetMemberValue(member, SubscriptionStatusAlias, "Active");
        SetMemberValue(member, SubscriptionExpiresUtcAlias, nextExpiry.ToString("O"));
        SetMemberValue(member, SubscriptionLastPaymentIntentIdAlias, request.PaymentIntentId ?? string.Empty);
        SetMemberValue(member, SubscriptionHistoryAlias, SerializeSubscriptionHistory(history));
        SetMemberValue(member, SubscriptionSummaryAlias, BuildSubscriptionSummary(history));
        _memberService.Save(member);

        return Task.FromResult(true);
    }

    public async Task<SubscriptionLifecycleRunResult> RunSubscriptionLifecycleAsync(CancellationToken cancellationToken = default)
    {
        var result = new SubscriptionLifecycleRunResult();

        const int pageSize = 500;
        var pageIndex = 0L;
        long totalRecords;

        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            var members = _memberService.GetAll(pageIndex, pageSize, out totalRecords).ToArray();

            foreach (var member in members)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var state = await EnsureMemberSubscriptionStateAsync(member, DateTime.UtcNow, sendExpiryWarningEmail: true, cancellationToken);
                if (!state.HasSubscriptionData)
                {
                    continue;
                }

                result.ProcessedMembers++;
                if (state.ExpiredMarked)
                {
                    result.ExpiredMarked++;
                }

                if (state.WarningEmailSent)
                {
                    result.WarningEmailsSent++;
                }
            }

            pageIndex++;
        }
        while (pageIndex * pageSize < totalRecords);

        return result;
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
        var utcNow = DateTime.UtcNow;
        history.RemoveAll(x => x.ContentKey == contentKey);
        history.Insert(0, new RecentlyViewedRecord
        {
            ContentKey = contentKey,
            ViewedAtUtc = utcNow,
            ProductName = ResolveTitle(content),
            ProductUrl = content.Url(),
            SectionName = ResolveSectionName(content)
        });

        if (history.Count > MaxRecentlyViewedItems)
        {
            var cutoffUtc = utcNow.AddMonths(-1);
            history = history
                .Where(x => x.ViewedAtUtc >= cutoffUtc)
                .OrderByDescending(x => x.ViewedAtUtc)
                .Take(MaxRecentlyViewedItems)
                .ToList();
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
        var ordered = history.OrderByDescending(x => x.PurchasedAtUtc).ToList();
        if (ordered.Count == 0)
        {
            return "<p>No purchases recorded.</p>";
        }

        var sb = new StringBuilder();
        sb.Append("<table><thead><tr>");
        sb.Append("<th>Product</th><th>Purchased (UTC)</th><th>Price</th><th>Public</th><th>Backoffice</th><th>Payment ID</th><th>Content Key</th>");
        sb.Append("</tr></thead><tbody>");

        foreach (var item in ordered)
        {
            sb.Append("<tr>");
            sb.Append("<td>").Append(HtmlEncode(item.ProductName ?? "Unknown")).Append("</td>");
            sb.Append("<td>").Append(item.PurchasedAtUtc.ToString("yyyy-MM-dd HH:mm")).Append("</td>");
            sb.Append("<td>").Append(item.PricePaid.HasValue ? $"${item.PricePaid.Value:0.00}" : "-").Append("</td>");
            sb.Append("<td>").Append(BuildHtmlLink(item.ProductUrl, "Open")).Append("</td>");
            sb.Append("<td>").Append(BuildHtmlLink(item.BackofficeUrl, "View")).Append("</td>");
            sb.Append("<td>").Append(HtmlEncode(string.IsNullOrWhiteSpace(item.PaymentIntentId) ? "-" : item.PaymentIntentId)).Append("</td>");
            sb.Append("<td><code>").Append(HtmlEncode(item.ContentKey.ToString())).Append("</code></td>");
            sb.Append("</tr>");
        }

        sb.Append("</tbody></table>");
        return sb.ToString();
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
        var ordered = history.OrderByDescending(x => x.ViewedAtUtc).ToList();
        if (ordered.Count == 0)
        {
            return "<p>No recently viewed products.</p>";
        }

        var sb = new StringBuilder();
        sb.Append("<table><thead><tr>");
        sb.Append("<th>Section</th><th>Product</th><th>Viewed (UTC)</th><th>Public</th><th>Content Key</th>");
        sb.Append("</tr></thead><tbody>");

        foreach (var item in ordered)
        {
            sb.Append("<tr>");
            sb.Append("<td>").Append(HtmlEncode(item.SectionName ?? "Content")).Append("</td>");
            sb.Append("<td>").Append(HtmlEncode(item.ProductName ?? "Unknown")).Append("</td>");
            sb.Append("<td>").Append(item.ViewedAtUtc.ToString("yyyy-MM-dd HH:mm")).Append("</td>");
            sb.Append("<td>").Append(BuildHtmlLink(item.ProductUrl, "Open")).Append("</td>");
            sb.Append("<td><code>").Append(HtmlEncode(item.ContentKey.ToString())).Append("</code></td>");
            sb.Append("</tr>");
        }

        sb.Append("</tbody></table>");
        return sb.ToString();
    }

    private static bool MemberHasActiveSubscription(Umbraco.Cms.Core.Models.IMember member, DateTime utcNow)
    {
        var expiresAtUtc = ParseUtc(GetMemberValue(member, SubscriptionExpiresUtcAlias));
        if (!expiresAtUtc.HasValue || expiresAtUtc.Value <= utcNow)
        {
            return false;
        }

        var status = GetMemberValue(member, SubscriptionStatusAlias);
        if (string.IsNullOrWhiteSpace(status))
        {
            return true;
        }

        return string.Equals(status, "active", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetMemberValue(Umbraco.Cms.Core.Models.IMember member, string alias)
    {
        if (member.Properties.All(x => !x.Alias.InvariantEquals(alias)))
        {
            return null;
        }

        return member.GetValue<string>(alias);
    }

    private static void SetMemberValue(Umbraco.Cms.Core.Models.IMember member, string alias, object? value)
    {
        if (member.Properties.All(x => !x.Alias.InvariantEquals(alias)))
        {
            return;
        }

        member.SetValue(alias, value);
    }

    private static DateTime? ParseUtc(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed.ToUniversalTime()
            : null;
    }

    private static List<SubscriptionRecord> ParseSubscriptionHistory(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new List<SubscriptionRecord>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<SubscriptionRecord>>(raw) ?? new List<SubscriptionRecord>();
        }
        catch
        {
            return new List<SubscriptionRecord>();
        }
    }

    private static string SerializeSubscriptionHistory(List<SubscriptionRecord> history)
    {
        return JsonSerializer.Serialize(history.OrderByDescending(x => x.ActivatedAtUtc));
    }

    private static string BuildSubscriptionSummary(IEnumerable<SubscriptionRecord> history)
    {
        var ordered = history.OrderByDescending(x => x.ActivatedAtUtc).ToList();
        if (ordered.Count == 0)
        {
            return "<p>No subscriptions recorded.</p>";
        }

        var sb = new StringBuilder();
        sb.Append("<table><thead><tr>");
        sb.Append("<th>Plan</th><th>Duration</th><th>Price</th><th>Activated (UTC)</th><th>Expires (UTC)</th><th>Warning Sent (UTC)</th><th>Payment ID</th>");
        sb.Append("</tr></thead><tbody>");

        foreach (var item in ordered)
        {
            sb.Append("<tr>");
            sb.Append("<td>").Append(HtmlEncode($"{item.PlanName} ({item.PlanCode})")).Append("</td>");
            sb.Append("<td>").Append(HtmlEncode(BuildDurationLabel(item.DurationMonths, item.DurationMinutes))).Append("</td>");
            sb.Append("<td>").Append($"${item.Price:0.00}").Append("</td>");
            sb.Append("<td>").Append(item.ActivatedAtUtc.ToString("yyyy-MM-dd HH:mm")).Append("</td>");
            sb.Append("<td>").Append(item.ExpiresAtUtc.ToString("yyyy-MM-dd HH:mm")).Append("</td>");
            sb.Append("<td>").Append(item.ExpiryWarningSentAtUtc.HasValue ? item.ExpiryWarningSentAtUtc.Value.ToString("yyyy-MM-dd HH:mm") : "-").Append("</td>");
            sb.Append("<td>").Append(HtmlEncode(string.IsNullOrWhiteSpace(item.PaymentIntentId) ? "-" : item.PaymentIntentId)).Append("</td>");
            sb.Append("</tr>");
        }

        sb.Append("</tbody></table>");
        return sb.ToString();
    }

    private static string BuildHtmlLink(string? url, string text)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "-";
        }

        var safeUrl = HtmlEncode(url);
        var safeText = HtmlEncode(text);
        return $"<a href=\"{safeUrl}\" target=\"_blank\" rel=\"noopener\">{safeText}</a>";
    }

    private static string HtmlEncode(string value) => WebUtility.HtmlEncode(value);

    private static string BuildDurationLabel(int durationMonths, int? durationMinutes)
    {
        if (durationMinutes.HasValue && durationMinutes.Value > 0)
        {
            return $"{durationMinutes.Value} minute(s)";
        }

        return $"{durationMonths} month(s)";
    }

    private static string BuildRemainingLabel(TimeSpan remaining, int daysRemaining)
    {
        if (remaining <= TimeSpan.Zero)
        {
            return "expired";
        }

        if (remaining.TotalHours < 1)
        {
            var minutes = Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes));
            return $"{minutes} minute(s)";
        }

        if (remaining.TotalDays < 1)
        {
            var hours = Math.Max(1, (int)Math.Ceiling(remaining.TotalHours));
            return $"{hours} hour(s)";
        }

        return $"{daysRemaining} day(s)";
    }

    private async Task<SubscriptionStateSnapshot> EnsureMemberSubscriptionStateAsync(Umbraco.Cms.Core.Models.IMember member, DateTime utcNow, bool sendExpiryWarningEmail, CancellationToken cancellationToken)
    {
        var planName = GetMemberValue(member, SubscriptionPlanAlias);
        var planCode = GetMemberValue(member, SubscriptionPlanAlias);
        var status = GetMemberValue(member, SubscriptionStatusAlias);
        var expiresAtUtc = ParseUtc(GetMemberValue(member, SubscriptionExpiresUtcAlias));
        var lastPaymentIntentId = GetMemberValue(member, SubscriptionLastPaymentIntentIdAlias);
        var history = ParseSubscriptionHistory(GetMemberValue(member, SubscriptionHistoryAlias));
        var currentFromHistory = history
            .OrderByDescending(x => x.ExpiresAtUtc)
            .ThenByDescending(x => x.ActivatedAtUtc)
            .FirstOrDefault();

        if (currentFromHistory != null)
        {
            planName ??= currentFromHistory.PlanName;
            planCode ??= currentFromHistory.PlanCode;
            expiresAtUtc ??= currentFromHistory.ExpiresAtUtc;
            lastPaymentIntentId ??= currentFromHistory.PaymentIntentId;
        }

        var hasSubscriptionData =
            !string.IsNullOrWhiteSpace(planName) ||
            !string.IsNullOrWhiteSpace(status) ||
            expiresAtUtc.HasValue ||
            !string.IsNullOrWhiteSpace(lastPaymentIntentId) ||
            history.Count > 0;

        if (!hasSubscriptionData)
        {
            return new SubscriptionStateSnapshot
            {
                HasSubscriptionData = false,
                IsActive = false,
                IsExpiringSoon = false,
                PlanName = planName,
                PlanCode = planCode,
                ExpiresAtUtc = expiresAtUtc,
                LastPaymentIntentId = lastPaymentIntentId
            };
        }

        var changed = false;
        var expiredMarked = false;
        var warningEmailSent = false;

        var isActive = MemberHasActiveSubscription(member, utcNow);
        if (!isActive && expiresAtUtc.HasValue && expiresAtUtc.Value <= utcNow && string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
        {
            SetMemberValue(member, SubscriptionStatusAlias, "Expired");
            status = "Expired";
            changed = true;
            expiredMarked = true;
        }

        int? daysRemaining = null;
        string? remainingLabel = null;
        var isExpiringSoon = false;

        if (isActive && expiresAtUtc.HasValue)
        {
            var remaining = expiresAtUtc.Value - utcNow;
            daysRemaining = Math.Max(0, (int)Math.Ceiling(remaining.TotalDays));
            remainingLabel = BuildRemainingLabel(remaining, daysRemaining.Value);
            isExpiringSoon = daysRemaining <= SubscriptionExpiryWarningDays;

            if (sendExpiryWarningEmail && isExpiringSoon)
            {
                var currentRecord = FindCurrentSubscriptionRecord(history, expiresAtUtc.Value, lastPaymentIntentId);
                if (currentRecord != null && !currentRecord.ExpiryWarningSentAtUtc.HasValue)
                {
                    try
                    {
                        await _membershipNotificationService.SendSubscriptionExpiringSoonAsync(
                            member.Key,
                            currentRecord.PlanName,
                            currentRecord.ExpiresAtUtc,
                            daysRemaining.Value,
                            cancellationToken);

                        currentRecord.ExpiryWarningSentAtUtc = utcNow;
                        changed = true;
                        warningEmailSent = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send subscription-expiring warning for member {MemberKey}", member.Key);
                    }
                }
            }
        }

        if (changed)
        {
            SetMemberValue(member, SubscriptionHistoryAlias, SerializeSubscriptionHistory(history));
            SetMemberValue(member, SubscriptionSummaryAlias, BuildSubscriptionSummary(history));
            _memberService.Save(member);
        }

        return new SubscriptionStateSnapshot
        {
            HasSubscriptionData = true,
            IsActive = isActive,
            IsExpiringSoon = isExpiringSoon,
            DaysRemaining = daysRemaining,
            RemainingLabel = remainingLabel,
            PlanName = planName,
            PlanCode = planCode,
            ExpiresAtUtc = expiresAtUtc,
            LastPaymentIntentId = lastPaymentIntentId,
            WasChanged = changed,
            ExpiredMarked = expiredMarked,
            WarningEmailSent = warningEmailSent
        };
    }

    private static SubscriptionRecord? FindCurrentSubscriptionRecord(IEnumerable<SubscriptionRecord> history, DateTime expiresAtUtc, string? paymentIntentId)
    {
        var byExpiry = history
            .Where(x => x.ExpiresAtUtc == expiresAtUtc)
            .OrderByDescending(x => x.ActivatedAtUtc)
            .ToList();

        if (byExpiry.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(paymentIntentId))
        {
            var byPaymentIntent = byExpiry.FirstOrDefault(x => string.Equals(x.PaymentIntentId, paymentIntentId, StringComparison.Ordinal));
            if (byPaymentIntent != null)
            {
                return byPaymentIntent;
            }
        }

        return byExpiry[0];
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

    private sealed class SubscriptionRecord
    {
        public string PlanCode { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public int DurationMonths { get; set; }
        public int? DurationMinutes { get; set; }
        public decimal Price { get; set; }
        public string? PaymentIntentId { get; set; }
        public DateTime ActivatedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime? ExpiryWarningSentAtUtc { get; set; }
    }

    private sealed class SubscriptionStateSnapshot
    {
        public bool HasSubscriptionData { get; init; }
        public bool IsActive { get; init; }
        public bool IsExpiringSoon { get; init; }
        public int? DaysRemaining { get; init; }
        public string? RemainingLabel { get; init; }
        public string? PlanCode { get; init; }
        public string? PlanName { get; init; }
        public DateTime? ExpiresAtUtc { get; init; }
        public string? LastPaymentIntentId { get; init; }
        public bool WasChanged { get; init; }
        public bool ExpiredMarked { get; init; }
        public bool WarningEmailSent { get; init; }
    }
}
