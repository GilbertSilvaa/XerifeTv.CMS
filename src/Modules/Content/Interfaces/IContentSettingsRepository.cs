using XerifeTv.CMS.Modules.Abstractions.Interfaces;

namespace XerifeTv.CMS.Modules.Content.Interfaces;

public interface IContentSettingsRepository
{
    Task CreateOrUpdateAsync(ContentSettingsEntity contentSettings);
    Task<ContentSettingsEntity?> GetContentSettingsAsync();
}