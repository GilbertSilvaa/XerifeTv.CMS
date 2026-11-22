using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Response;
using XerifeTv.CMS.Modules.Series;

namespace XerifeTv.CMS.Modules.Series.Views.Models;

public record EpisodeFormModelView(
    Episode Episode,
    IEnumerable<GetMediaDeliveryProfileResponseDto> MediaDeliveryProfiles);
