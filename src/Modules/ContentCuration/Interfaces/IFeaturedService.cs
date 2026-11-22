using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.ContentCuration.Dtos.Request;
using XerifeTv.CMS.Modules.ContentCuration.Dtos.Response;

namespace XerifeTv.CMS.Modules.ContentCuration.Interfaces;

public interface IFeaturedService
{
    Task<Result<PagedList<GetFeaturedResponseDto>>> GetAsync(int currentPage, int limit);
    Task<Result<GetFeaturedResponseDto?>> GetAsync(string id);
    Task<Result<IReadOnlyList<GetFeaturedResponseDto>>> GetAllAsync(bool onlyActive);
    Task<Result<string>> CreateAsync(SaveFeaturedRequestDto dto);
    Task<Result<string>> UpdateAsync(SaveFeaturedRequestDto dto);
    Task<Result<bool>> DeleteAsync(string id);
    Task<Result<bool>> ToggleActiveAsync(string id);
    Task<Result<bool>> MoveAsync(string id, int direction);
    Task<Result<GetFeaturedResponseDto?>> GetHomeHeroAsync();
}
