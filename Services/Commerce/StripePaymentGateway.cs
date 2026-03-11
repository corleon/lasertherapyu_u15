using LTU_U15.Models.Commerce;
using LTU_U15.Services.Site;
using Stripe;
using Stripe.Checkout;
using System.Globalization;

namespace LTU_U15.Services.Commerce;

public sealed class StripePaymentGateway : IStripePaymentGateway
{
    private readonly ISiteSettingsService _siteSettingsService;

    public StripePaymentGateway(ISiteSettingsService siteSettingsService)
    {
        _siteSettingsService = siteSettingsService;
    }

    public async Task<string> CreateCheckoutUrlAsync(StripeCheckoutRequest request, CancellationToken cancellationToken = default)
    {
        var settings = (await _siteSettingsService.GetAsync(cancellationToken)).Stripe;

        if (string.IsNullOrWhiteSpace(settings.SecretKey))
        {
            throw new InvalidOperationException("Stripe SecretKey is not configured.");
        }

        if (request.Items == null || request.Items.Count == 0)
        {
            throw new InvalidOperationException("Stripe checkout requires at least one line item.");
        }

        StripeConfiguration.ApiKey = settings.SecretKey;

        var customerId = await EnsureCustomerAsync(request, cancellationToken);
        var contentKeys = request.Items.Select(x => x.ContentKey).Distinct().ToArray();

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            Customer = customerId,
            CustomerEmail = string.IsNullOrWhiteSpace(customerId) ? request.CustomerEmail : null,
            BillingAddressCollection = "auto",
            PaymentMethodTypes = new List<string> { "card" },
            Metadata = new Dictionary<string, string>
            {
                ["memberKey"] = request.MemberKey.ToString(),
                ["contentKey"] = contentKeys[0].ToString(),
                ["contentKeys"] = string.Join(",", contentKeys)
            },
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    ["memberKey"] = request.MemberKey.ToString(),
                    ["contentKey"] = contentKeys[0].ToString(),
                    ["contentKeys"] = string.Join(",", contentKeys)
                }
            },
            LineItems = request.Items.Select(item => new SessionLineItemOptions
            {
                Quantity = 1,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = request.Currency,
                    UnitAmount = (long)Math.Round(item.Price * 100m, MidpointRounding.AwayFromZero),
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = item.ProductName,
                        Description = $"Payment for: {item.ProductName}"
                    }
                }
            }).ToList()
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(session.Url))
        {
            throw new InvalidOperationException("Stripe did not return Checkout URL.");
        }

        return session.Url;
    }

    public StripeCheckoutCompletedEvent? ParseCheckoutCompletedEvent(string payload)
    {
        var stripeEvent = EventUtility.ParseEvent(payload, throwOnApiVersionMismatch: false);
        if (!string.Equals(stripeEvent.Type, "payment_intent.succeeded", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent?.Metadata == null)
        {
            throw new InvalidOperationException("Stripe payment intent metadata is missing.");
        }

        if (!paymentIntent.Metadata.TryGetValue("memberKey", out var memberKeyRaw) || !Guid.TryParse(memberKeyRaw, out var memberKey))
        {
            throw new InvalidOperationException("Stripe payment intent has invalid memberKey metadata.");
        }

        if (!paymentIntent.Metadata.TryGetValue("contentKeys", out var contentKeysRaw) || string.IsNullOrWhiteSpace(contentKeysRaw))
        {
            if (!paymentIntent.Metadata.TryGetValue("contentKey", out var contentKeyRaw) || string.IsNullOrWhiteSpace(contentKeyRaw))
            {
                throw new InvalidOperationException("Stripe payment intent has invalid content metadata.");
            }

            contentKeysRaw = contentKeyRaw;
        }

        var contentKeys = contentKeysRaw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => Guid.TryParse(x, out var key) ? key : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        if (contentKeys.Length == 0)
        {
            throw new InvalidOperationException("Stripe payment intent has invalid content metadata.");
        }

        return new StripeCheckoutCompletedEvent
        {
            MemberKey = memberKey,
            ContentKeys = contentKeys,
            PaymentStatus = paymentIntent.Status ?? string.Empty,
            PaymentIntentId = paymentIntent.Id
        };
    }

    private static string BuildCustomerName(StripeCheckoutRequest request)
    {
        return string.Join(" ", new[] { request.CustomerFirstName, request.CustomerLastName }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private async Task<string?> EnsureCustomerAsync(StripeCheckoutRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerEmail))
        {
            return null;
        }

        var customerService = new CustomerService();
        var existingCustomers = await customerService.ListAsync(new CustomerListOptions
        {
            Email = request.CustomerEmail,
            Limit = 1
        }, cancellationToken: cancellationToken);

        var customer = existingCustomers.Data.FirstOrDefault();
        var address = BuildStripeAddress(request);
        var customerName = BuildCustomerName(request);

        if (customer == null)
        {
            customer = await customerService.CreateAsync(new CustomerCreateOptions
            {
                Email = request.CustomerEmail,
                Name = string.IsNullOrWhiteSpace(customerName) ? null : customerName,
                Phone = request.CustomerPhoneNumber,
                Address = address
            }, cancellationToken: cancellationToken);

            return customer.Id;
        }

        await customerService.UpdateAsync(customer.Id, new CustomerUpdateOptions
        {
            Name = string.IsNullOrWhiteSpace(customerName) ? null : customerName,
            Phone = request.CustomerPhoneNumber,
            Address = address
        }, cancellationToken: cancellationToken);

        return customer.Id;
    }

    private static AddressOptions? BuildStripeAddress(StripeCheckoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerAddress) &&
            string.IsNullOrWhiteSpace(request.CustomerCity) &&
            string.IsNullOrWhiteSpace(request.CustomerState) &&
            string.IsNullOrWhiteSpace(request.CustomerZipPostalCode) &&
            string.IsNullOrWhiteSpace(request.CustomerCountry))
        {
            return null;
        }

        var countryCode = TryMapCountryCode(request.CustomerCountry);

        return new AddressOptions
        {
            Line1 = request.CustomerAddress,
            Line2 = request.CustomerAddress2,
            City = request.CustomerCity,
            State = request.CustomerState,
            PostalCode = request.CustomerZipPostalCode,
            Country = countryCode
        };
    }

    private static string? TryMapCountryCode(string? country)
    {
        if (string.IsNullOrWhiteSpace(country))
        {
            return null;
        }

        var trimmed = country.Trim();
        if (trimmed.Length == 2)
        {
            return trimmed.ToUpperInvariant();
        }

        try
        {
            return CultureInfo
                .GetCultures(CultureTypes.SpecificCultures)
                .Select(c => new RegionInfo(c.Name))
                .GroupBy(r => r.TwoLetterISORegionName)
                .Select(g => g.First())
                .FirstOrDefault(r =>
                    string.Equals(r.EnglishName, trimmed, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(r.NativeName, trimmed, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(r.DisplayName, trimmed, StringComparison.OrdinalIgnoreCase))
                ?.TwoLetterISORegionName;
        }
        catch
        {
            return null;
        }
    }
}
