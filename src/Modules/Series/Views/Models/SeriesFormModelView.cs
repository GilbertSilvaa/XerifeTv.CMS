using XerifeTv.CMS.Modules.Franchise.Dtos.Response;
using XerifeTv.CMS.Modules.Series.Dtos.Response;

namespace XerifeTv.CMS.Modules.Series.Views.Models;

public sealed record SeriesFormModelView(
    GetSeriesResponseDto? SeriesDto,
    IEnumerable<GetFranchiseResponseDto> Franchises,
    string? SelectedFranchiseName);
