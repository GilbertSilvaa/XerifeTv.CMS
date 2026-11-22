using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Response;
using XerifeTv.CMS.Modules.Series.Dtos.Response;

namespace XerifeTv.CMS.Modules.Series.Views.Models;

public record EpisodesModelView(
    GetEpisodesResponseDto? EpisodesDto,
    IEnumerable<GetMediaDeliveryProfileResponseDto> MediaDeliveryProfiles);
