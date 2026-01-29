using XerifeTv.CMS.Modules.Abstractions.Interfaces;

namespace XerifeTv.CMS.Modules.Media.Delivery.Intefaces;

public interface IMediaDeliveryProfileRepository : IBaseRepository<MediaDeliveryProfileEntity>
{
    Task<IEnumerable<MediaDeliveryProfileEntity>> GetAsync(bool isIncludeDisabled = false);
    Task<MediaDeliveryProfileEntity?> GetByNameAsync(string name);
}
