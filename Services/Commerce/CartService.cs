using System.Text.Json;
using LTU_U15.Models.Commerce;
using Microsoft.AspNetCore.Http;

namespace LTU_U15.Services.Commerce;

public sealed class CartService : ICartService
{
    private const string CartCookieName = "ltu_cart";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IContentPurchaseService _purchaseService;

    public CartService(IHttpContextAccessor httpContextAccessor, IContentPurchaseService purchaseService)
    {
        _httpContextAccessor = httpContextAccessor;
        _purchaseService = purchaseService;
    }

    public async Task<CartSummaryModel> GetCartSummaryAsync(CancellationToken cancellationToken = default)
    {
        var contentKeys = ReadCartKeys();
        if (await _purchaseService.CurrentMemberHasActiveSubscriptionAsync(cancellationToken))
        {
            if (contentKeys.Count > 0)
            {
                WriteCartKeys(Array.Empty<Guid>());
            }

            return new CartSummaryModel();
        }

        var purchasedKeys = await _purchaseService.GetCurrentMemberPurchasedKeysAsync(cancellationToken);
        if (purchasedKeys.Count > 0)
        {
            var filteredKeys = contentKeys.Where(x => !purchasedKeys.Contains(x)).ToList();
            if (filteredKeys.Count != contentKeys.Count)
            {
                contentKeys = filteredKeys;
                WriteCartKeys(contentKeys);
            }
        }

        if (contentKeys.Count == 0)
        {
            return new CartSummaryModel();
        }

        var items = new List<CartItemModel>();
        foreach (var contentKey in contentKeys)
        {
            var content = await _purchaseService.GetPayableContentAsync(contentKey, cancellationToken);
            if (content == null || !content.IsPaid || !content.Price.HasValue)
            {
                continue;
            }

            items.Add(new CartItemModel
            {
                ContentKey = content.ContentKey,
                Name = content.Name,
                Url = content.Url,
                ContentTypeAlias = content.ContentTypeAlias,
                Price = content.Price.Value
            });
        }

        var validKeys = items.Select(x => x.ContentKey).ToList();
        if (validKeys.Count != contentKeys.Count)
        {
            WriteCartKeys(validKeys);
        }

        return new CartSummaryModel
        {
            Items = items
        };
    }

    public async Task<bool> AddAsync(Guid contentKey, CancellationToken cancellationToken = default)
    {
        if (contentKey == Guid.Empty)
        {
            return false;
        }

        var content = await _purchaseService.GetPayableContentAsync(contentKey, cancellationToken);
        if (content == null || !content.IsPaid)
        {
            return false;
        }

        var keys = ReadCartKeys();
        if (keys.Contains(contentKey))
        {
            return true;
        }

        keys.Add(contentKey);
        WriteCartKeys(keys);
        return true;
    }

    public Task RemoveAsync(Guid contentKey, CancellationToken cancellationToken = default)
    {
        var keys = ReadCartKeys();
        keys.RemoveAll(x => x == contentKey);
        WriteCartKeys(keys);
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        WriteCartKeys(Array.Empty<Guid>());
        return Task.CompletedTask;
    }

    private List<Guid> ReadCartKeys()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return new List<Guid>();
        }

        if (context.Items.TryGetValue(CartCookieName, out var cached) && cached is List<Guid> cachedKeys)
        {
            return new List<Guid>(cachedKeys);
        }

        if (!context.Request.Cookies.TryGetValue(CartCookieName, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return new List<Guid>();
        }

        try
        {
            var keys = JsonSerializer.Deserialize<List<Guid>>(raw);
            return keys?.Where(x => x != Guid.Empty).Distinct().ToList() ?? new List<Guid>();
        }
        catch
        {
            return new List<Guid>();
        }
    }

    private void WriteCartKeys(IEnumerable<Guid> contentKeys)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return;
        }

        var normalized = contentKeys
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        if (normalized.Count == 0)
        {
            context.Items[CartCookieName] = new List<Guid>();
            context.Response.Cookies.Delete(CartCookieName);
            return;
        }

        context.Items[CartCookieName] = normalized;

        context.Response.Cookies.Append(
            CartCookieName,
            JsonSerializer.Serialize(normalized),
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddDays(14)
            });
    }
}
