using MongoDB.Driver.Linq;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Franchise.Interfaces;
using XerifeTv.CMS.Modules.Series.Dtos.Request;
using XerifeTv.CMS.Modules.Series.Dtos.Response;
using XerifeTv.CMS.Modules.Series.Interfaces;
using XerifeTv.CMS.Modules.Series.Specifications;

namespace XerifeTv.CMS.Modules.Series;

public class SeriesService(
    ISeriesRepository repository,
    IFranchiseService franchiseService,
    IBackgroundJobQueueService backgroundJobQueueService,
    IConfiguration configuration) : ISeriesService
{
    public async Task<Result<PagedList<GetSeriesResponseDto>>> GetAsync(int currentPage, int limit)
    {
        try
        {
            var response = await repository.GetAsync(currentPage, limit);

            var result = new PagedList<GetSeriesResponseDto>(
              response.CurrentPage,
              response.TotalPageCount,
              response.Items.Select(GetSeriesResponseDto.FromEntity));

            return Result<PagedList<GetSeriesResponseDto>>.Success(result);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<PagedList<GetSeriesResponseDto>>.Failure(error);
        }
    }

    public async Task<Result<GetSeriesResponseDto?>> GetAsync(string id)
    {
        try
        {
            var response = await repository.GetAsync(id);

            if (response is null)
                return Result<GetSeriesResponseDto?>
                  .Failure(new Error("404", "Conteúdo não encontrado"));

            return Result<GetSeriesResponseDto?>
              .Success(GetSeriesResponseDto.FromEntity(response));
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<GetSeriesResponseDto?>.Failure(error);
        }
    }

    public async Task<Result<GetSeriesResponseDto?>> GetByImdbIdAsync(string imdbId)
    {
        try
        {
            var response = await repository.GetByImdbIdAsync(imdbId);

            if (response is null)
                return Result<GetSeriesResponseDto?>
                  .Failure(new Error("404", "Conteúdo não encontrado"));

            return Result<GetSeriesResponseDto?>
              .Success(GetSeriesResponseDto.FromEntity(response));
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<GetSeriesResponseDto?>.Failure(error);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateSeriesRequestDto dto)
    {
        try
        {
            var entity = dto.ToEntity();
            var imdbIdSpec = new UniqueImdbIdSpecification(repository);

            if (!await imdbIdSpec.IsSatisfiedByAsync(entity))
                return Result<string>.Failure(
                  new Error("409", $"Série não cadastrada. Imdb ID {entity.ImdbId} duplicado"));

            var response = await repository.CreateAsync(entity);
            await backgroundJobQueueService.AddJobInQueueAsync(ECalculateCategoryDistributionJobQueueType.SERIES);
            await backgroundJobQueueService.AddJobInQueueAsync(EDispatchWebhooksJobQueueType.SERIES, response!);

            return Result<string>.Success(entity.Id);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<string>.Failure(error);
        }
    }

    public async Task<Result<string>> UpdateAsync(UpdateSeriesRequestDto dto)
    {
        try
        {
            var entity = dto.ToEntity();
            var response = await repository.GetAsync(entity.Id);

            if (response is null)
                return Result<string>.Failure(new Error("404", "Conteúdo não encontrado"));

            var imdbIdSpec = new UniqueImdbIdSpecification(repository);

            if (!await imdbIdSpec.IsSatisfiedByAsync(entity))
                return Result<string>.Failure(
                  new Error("409", $"Série não atualizada. Imdb ID {entity.ImdbId} duplicado"));

            await repository.UpdateAsync(entity);
            await backgroundJobQueueService.AddJobInQueueAsync(ECalculateCategoryDistributionJobQueueType.SERIES);

            return Result<string>.Success(entity.Id);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<string>.Failure(error);
        }
    }

    public async Task<Result<bool>> DeleteAsync(string id)
    {
        try
        {
            var response = await repository.GetAsync(id);

            if (response is null)
                return Result<bool>.Failure(new Error("404", "Conteúdo não encontrado"));

            await repository.DeleteAsync(id);
            await backgroundJobQueueService.AddJobInQueueAsync(ECalculateCategoryDistributionJobQueueType.SERIES);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<bool>.Failure(error);
        }
    }

    public async Task<Result<PagedList<GetSeriesResponseDto>>> GetByFilterAsync(GetSeriesByFilterRequestDto dto)
    {
        try
        {
            if (dto.Filter == Enums.ESeriesSearchFilter.FRANCHISE)
            {
                var franchiseResult = await franchiseService.GetByNameAsync(dto.Search);

                if (franchiseResult.IsFailure)
                    return Result<PagedList<GetSeriesResponseDto>>.Success(new PagedList<GetSeriesResponseDto>(0, 0, []));

                dto.Search = franchiseResult.Data!.Id;
            }

            var response = await repository.GetByFilterAsync(dto);

            var result = new PagedList<GetSeriesResponseDto>(
              response.CurrentPage,
              response.TotalPageCount,
              response.Items.Select(GetSeriesResponseDto.FromEntity));

            return Result<PagedList<GetSeriesResponseDto>>.Success(result);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<PagedList<GetSeriesResponseDto>>.Failure(error);
        }
    }

    public async Task<Result<GetEpisodesResponseDto>> GetEpisodesBySeasonAsync(
      string serieId, int season, bool includeDisabled, int? specificEpisode = null)
    {
        try
        {
            var response = await repository.GetEpisodesBySeasonAsync(serieId, season, includeDisabled, specificEpisode);

            if (response is null)
                return Result<GetEpisodesResponseDto>
                  .Failure(new Error("404", "Conteúdo não encontrado"));

            var result = GetEpisodesResponseDto.FromEntity(response);
            result.SetUrlResolverPathEpisodes(configuration["SecuritySettings:ContentEncryptionKey"]!);

            return Result<GetEpisodesResponseDto>.Success(result);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<GetEpisodesResponseDto>.Failure(error);
        }
    }

    public async Task<Result<Episode?>> GetEpisodeBySeriesIdAndEpisodeIdAsync(string seriesId, string episodeId)
    {
        try
        {
            var response = await repository.GetEpisodeBySeriesIdAndEpisodeIdAsync(seriesId, episodeId);

            if (response is null)
                return Result<Episode?>.Failure(new Error("404", "Conteúdo não encontrado"));

            response.SetUrlResolverPath(configuration["SecuritySettings:ContentEncryptionKey"]!);

            return Result<Episode?>.Success(response);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<Episode?>.Failure(error);
        }
    }

    public async Task<Result<string>> CreateEpisodeAsync(CreateEpisodeRequestDto dto)
    {
        try
        {
            var seriesResponse = await repository.GetAsync(dto.SerieId);

            if (seriesResponse is null)
                return Result<string>.Failure(new Error("404", "Conteúdo não encontrado"));

            var episodesResult = await GetEpisodesBySeasonAsync(dto.SerieId, dto.Season, includeDisabled: true);
            if (episodesResult.IsFailure)
                return Result<string>.Failure(episodesResult.Error);

            var existingEpisode = episodesResult.Data?.Episodes?
                .Any(e => e.Season == dto.Season && e.Number == dto.Number) ?? false;

            if (existingEpisode)
                return Result<string>.Failure(
                    new Error("409", $"Episódio não cadastrado. [{seriesResponse.ImdbId}|T{dto.Season}:EP{dto.Number}] duplicado"));

            await repository.CreateEpisodeAsync(seriesResponse.Id, dto.ToEntity());

            return Result<string>.Success(dto.ToEntity().Id);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<string>.Failure(error);
        }
    }

    public async Task<Result<string>> UpdateEpisodeAsync(UpdateEpisodeRequestDto dto)
    {
        try
        {
            var seriesResponse = await repository.GetAsync(dto.SerieId);

            if (seriesResponse is null)
                return Result<string>.Failure(new Error("404", "Conteúdo não encontrado"));

            var episodesResult = await GetEpisodesBySeasonAsync(dto.SerieId, dto.Season, includeDisabled: true);
            if (episodesResult.IsFailure)
                return Result<string>.Failure(episodesResult.Error);

            var existingEpisode = episodesResult.Data?.Episodes?
                .Any(e => e.Season == dto.Season && e.Number == dto.Number && e.Id != dto.Id) ?? false;

            if (existingEpisode)
                return Result<string>.Failure(
                    new Error("409", $"Episódio não atualizado. [{seriesResponse.ImdbId}|T{dto.Season}:EP{dto.Number}] duplicado"));

            await repository.UpdateEpisodeAsync(seriesResponse.Id, dto.ToEntity());

            return Result<string>.Success(seriesResponse.Id);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<string>.Failure(error);
        }
    }

    public async Task<Result<bool>> DeleteEpisodeAsync(string serieId, string id)
    {
        try
        {
            var response = await repository.GetAsync(serieId);

            if (response is null)
                return Result<bool>.Failure(new Error("404", "Série não encontrada"));

            await repository.DeleteEpisodeAsync(serieId, id);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<bool>.Failure(error);
        }
    }
}

