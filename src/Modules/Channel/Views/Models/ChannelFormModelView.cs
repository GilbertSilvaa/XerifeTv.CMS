using XerifeTv.CMS.Modules.Channel.Dtos.Response;
using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Response;

namespace XerifeTv.CMS.Modules.Channel.Views.Models;

public record ChannelFormModelView(
    GetChannelResponseDto? ChannelDto,
    IEnumerable<GetMediaDeliveryProfileResponseDto> MediaDeliveryProfiles);
