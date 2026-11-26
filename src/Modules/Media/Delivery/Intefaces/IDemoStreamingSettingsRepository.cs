using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Response;

namespace XerifeTv.CMS.Modules.Media.Delivery.Intefaces;

public interface IDemoStreamingSettingsRepository
{
    Task<DemoStreamingSettingsEntity> GetSettingsAsync();
    Task CreateOrUpdateAsync(DemoStreamingSettingsEntity settings);
}
