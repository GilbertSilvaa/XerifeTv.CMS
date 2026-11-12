using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.People.Dtos.Request;
using XerifeTv.CMS.Modules.People.Dtos.Response;

namespace XerifeTv.CMS.Modules.People.Interfaces;

public interface IPersonService
{
    Task<Result<GetPersonResponseDto?>> GetAsync(string id);
    Task<Result<GetPersonResponseDto>> UpdateAsync(UpdatePersonRequestDto dto);
    Task<Result<IEnumerable<PersonSearchResultDto>>> SearchTmdbAsync(string name);
    Task<Result<GetPersonResponseDto>> ImportPersonAsync(int tmdbId);
    Task<Result<GetPersonResponseDto>> RefreshFromTmdbAsync(string id);
    Task<Result<PersonUpsertResult>> EnsureLocalPersonFromTmdbAsync(
        int tmdbId,
        string fallbackName,
        string? fallbackProfileUrl);
    Task<Result<IReadOnlyCollection<PersonEntity>>> GetByTmdbIdsAsync(IEnumerable<int> tmdbIds);
}
