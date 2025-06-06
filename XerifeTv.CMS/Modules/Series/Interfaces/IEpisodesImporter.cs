using XerifeTv.CMS.Modules.Common;

namespace XerifeTv.CMS.Modules.Series.Interfaces;

public interface IEpisodesImporter
{
    Task<Result<bool>> ImportEpisodesAsync(string seriesId);
}
