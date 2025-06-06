using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Integrations.Imdb.Services;
using XerifeTv.CMS.Modules.Series.Dtos.Request;
using XerifeTv.CMS.Modules.Series.Interfaces;

namespace XerifeTv.CMS.Modules.Series.Importers;

public class EpisodesImdbImporter(
    ISeriesService _service,
    IImdbService _imdbService) : IEpisodesImporter
{
    public async Task<Result<bool>> ImportEpisodesAsync(string seriesId)
    {
        try
        {
            var seriesResult = await _service.Get(seriesId);
            if (seriesResult.IsFailure) Result<bool>.Failure(seriesResult.Error);

            for (int i = 1; i <= seriesResult.Data?.NumberSeasons; i++)
            {
                var episodesBySeasonResult = await _service.GetEpisodesBySeason(seriesId, i, includeDisabled: true);
                if (episodesBySeasonResult.IsFailure) continue;
                if (episodesBySeasonResult.Data?.Episodes.Count() > 0) continue;

                var result = await _imdbService.GetSeriesEpisodesBySeasonAsync(seriesResult.Data.ImdbId, i);
                if (result.IsFailure || result.Data == null) continue;

                foreach (var episode in result.Data.Episodes)
                {
                    var durationInMinutes = episode.Runtime ?? 0;
                    long durationInSeconds = durationInMinutes * 60L;

                    await _service.CreateEpisode(new CreateEpisodeRequestDto
                    {
                        SerieId = seriesResult.Data.Id,
                        Title = episode.Name,
                        BannerUrl = episode.BannerUrl,
                        Number = episode.EpisodeNumber,
                        Season = episode.SeasonNumber,
                        VideoDuration = durationInSeconds
                    });
                }
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<bool>.Failure(error);
        }
    }
}
