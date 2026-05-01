using XerifeTv.CMS.Modules.Franchise.Dtos.Response;

namespace XerifeTv.CMS.Views.Shared.Models;

public sealed record FranchiseFieldViewModel(
    string IdPrefix,
    string? FranchiseId,
    string? FranchiseName,
    IEnumerable<GetFranchiseResponseDto> Franchises);
