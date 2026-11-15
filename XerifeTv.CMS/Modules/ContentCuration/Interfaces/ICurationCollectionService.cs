using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.ContentCuration.Dtos.Request;
using XerifeTv.CMS.Modules.ContentCuration.Dtos.Response;
using XerifeTv.CMS.Modules.ContentCuration.Entities;

namespace XerifeTv.CMS.Modules.ContentCuration.Interfaces;

public interface ICurationCollectionService
{
    Task<Result<PagedList<GetCurationCollectionResponseDto>>> GetAsync(int currentPage, int limit);
    Task<Result<GetCurationCollectionResponseDto?>> GetAsync(string id);
    Task<Result<IReadOnlyList<GetCurationCollectionResponseDto>>> GetAllAsync(bool onlyActive);
    Task<Result<string>> CreateAsync(SaveCurationCollectionRequestDto dto);
    Task<Result<string>> UpdateAsync(SaveCurationCollectionRequestDto dto);
    Task<Result<bool>> DeleteAsync(string id);
    Task<Result<bool>> ToggleActiveAsync(string id);
    Task<Result<bool>> MoveAsync(string id, int direction);
    Task<Result<CurationCollectionPreviewResponseDto?>> PreviewAsync(string id);
    Task<Result<IReadOnlyList<GetCurationContentResponseDto>>> SearchContentsAsync(SearchCurationContentRequestDto dto);
    Task<Result<CurationReorderResponseDto>> ReorderAutomaticallyAsync();
    Task<Result<string[]>> GetActiveCategoryNamesAsync();
    Task<Result<CurationCollectionEntity?>> GetActiveByNameAsync(string name);
}
