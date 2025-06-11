using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Series.Dtos.Response;

namespace XerifeTv.CMS.Modules.Series.Interfaces;

public interface IEpisodesImporter
{
    Task<Result<string>> ImportAsync(string seriesId);
	Task<Result<ImportEpisodesResponseDto>> MonitorImportAsync(string importId);
}
