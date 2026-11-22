using XerifeTv.CMS.Modules.Franchise.Dtos.Response;

namespace XerifeTv.CMS.Shared.Views.Models;

public sealed record FranchiseFieldViewModel(
    string IdPrefix,
    string? FranchiseId,
    string? FranchiseName,
    IEnumerable<GetFranchiseResponseDto> Franchises);
