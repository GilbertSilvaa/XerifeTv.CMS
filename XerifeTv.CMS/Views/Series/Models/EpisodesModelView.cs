using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Response;
using XerifeTv.CMS.Modules.Series.Dtos.Response;

namespace XerifeTv.CMS.Views.Series.Models;

public record EpisodesModelView(
    GetEpisodesResponseDto? EpisodesDto,
    IEnumerable<GetMediaDeliveryProfileResponseDto> MediaDeliveryProfiles);
