using XerifeTv.CMS.Modules.Franchise.Dtos.Response;
using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Response;
using XerifeTv.CMS.Modules.Movie.Dtos.Response;

namespace XerifeTv.CMS.Modules.Movie.Views.Models;

public sealed record MovieFormModelView(
    GetMovieResponseDto? MovieDto,
    IEnumerable<GetMediaDeliveryProfileResponseDto> MediaDeliveryProfiles,
    IEnumerable<GetFranchiseResponseDto> Franchises,
    string? SelectedFranchiseName);
