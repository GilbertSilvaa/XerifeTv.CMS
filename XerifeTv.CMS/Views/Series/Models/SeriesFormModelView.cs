using XerifeTv.CMS.Modules.Franchise.Dtos.Response;
using XerifeTv.CMS.Modules.Series.Dtos.Response;

namespace XerifeTv.CMS.Views.Series.Models;

public sealed record SeriesFormModelView(
    GetSeriesResponseDto? SeriesDto,
    IEnumerable<GetFranchiseResponseDto> Franchises,
    string? SelectedFranchiseName);
