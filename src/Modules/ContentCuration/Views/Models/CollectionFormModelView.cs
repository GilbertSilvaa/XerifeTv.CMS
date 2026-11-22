using XerifeTv.CMS.Modules.ContentCuration.Dtos.Response;
using XerifeTv.CMS.Modules.Franchise.Dtos.Response;

namespace XerifeTv.CMS.Modules.ContentCuration.Views.Models;

public sealed record CollectionFormModelView(
    GetCurationCollectionResponseDto? Collection,
    IReadOnlyList<GetFranchiseResponseDto> Franchises);
