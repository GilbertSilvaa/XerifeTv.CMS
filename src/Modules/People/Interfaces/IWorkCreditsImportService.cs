using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.People.Dtos.Request;
using XerifeTv.CMS.Modules.People.Dtos.Response;

namespace XerifeTv.CMS.Modules.People.Interfaces;

public interface IWorkCreditsImportService
{
    Task<Result<ImportWorkCreditsResponseDto>> ImportMovieCreditsAsync(ImportWorkCreditsRequestDto dto);
    Task<Result<ImportWorkCreditsResponseDto>> ImportSeriesCreditsAsync(ImportWorkCreditsRequestDto dto);
}
