using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.ContentCuration.Dtos.Response;

namespace XerifeTv.CMS.Modules.ContentCuration.Interfaces;

public interface ICurationCompositionService
{
    Task<Result<GetCurationHomeV2ResponseDto>> GetHomeAsync();
    Task<Result<GetCurationCategoryV2ResponseDto?>> GetCategoryAsync(string category, int page = 1, int pageSize = 0);
    Task<long> GetCacheVersionAsync();
}
