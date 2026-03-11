using LTU_U15.Models.Commerce;

namespace LTU_U15.Services.Commerce;

public interface ICartService
{
    Task<CartSummaryModel> GetCartSummaryAsync(CancellationToken cancellationToken = default);
    Task<bool> AddAsync(Guid contentKey, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid contentKey, CancellationToken cancellationToken = default);
    Task ClearAsync();
}
