using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Response;
using XerifeTv.CMS.Modules.Movie.Dtos.Response;

namespace XerifeTv.CMS.Views.Movies.Models;

public sealed record MovieFormModelView(
    GetMovieResponseDto? MovieDto,
    IEnumerable<GetMediaDeliveryProfileResponseDto> MediaDeliveryProfiles);
