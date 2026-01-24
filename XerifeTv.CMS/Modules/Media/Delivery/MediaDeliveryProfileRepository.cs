using Microsoft.Extensions.Options;
using XerifeTv.CMS.Modules.Abstractions.Repositories;
using XerifeTv.CMS.Modules.Media.Delivery.Intefaces;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.Media.Delivery;

public class MediaDeliveryProfileRepository : BaseRepository<MediaDeliveryProfileEntity>, IMediaDeliveryProfileRepository
{
    public MediaDeliveryProfileRepository(IOptions<DBSettings> dbSettings) : base(ECollection.MEDIA_DELIVERY_PROFILES, dbSettings) { }
}
