using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.ContentCuration.Entities;

namespace XerifeTv.CMS.Modules.ContentCuration.Interfaces;

public interface ICurationSettingsRepository : IBaseRepository<CurationSettingsEntity>
{
    Task<CurationSettingsEntity> GetSettingsAsync();
    Task<long> GetVersionAsync();
    Task IncrementVersionAsync();
}
