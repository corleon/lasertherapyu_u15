using LTU_U15.Models.Site;

namespace LTU_U15.Services.Site;

public interface ISiteSettingsService
{
    Task<SiteRuntimeSettings> GetAsync(CancellationToken cancellationToken = default);
}
