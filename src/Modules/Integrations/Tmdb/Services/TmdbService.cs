using Newtonsoft.Json;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Integrations.Tmdb.Dtos;

namespace XerifeTv.CMS.Modules.Integrations.Tmdb.Services;

public class TmdbService(IConfiguration configuration, ICacheService cacheService) : ITmdbService
{
    private const string BaseUrl = "https://api.themoviedb.org/3";
    private static readonly TimeSpan SearchCacheTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ExternalIdsCacheTtl = TimeSpan.FromHours(12);
    private static readonly TimeSpan EpisodesCacheTtl = TimeSpan.FromMinutes(10);

    public async Task<Result<GetAllResultsByImdbIdResponseDto?>> GetAllResultsByImdbIdAsync(string imdbId)
    {
        var result = await GetAsync<GetAllResultsByImdbIdResponseDto>(
            $"{BaseUrl}/find/{imdbId}?external_source=imdb_id");

        if (result.IsFailure)
            return Result<GetAllResultsByImdbIdResponseDto?>.Failure(result.Error);

        return Result<GetAllResultsByImdbIdResponseDto?>.Success(result.Data);
    }

    public async Task<Result<GetMovieByImdbResponseDto?>> GetMovieByImdbIdAsync(string imdbId)
    {
        try
        {
            var allResults = await GetAllResultsByImdbIdAsync(imdbId);
            if (allResults.IsFailure)
                return Result<GetMovieByImdbResponseDto?>.Failure(allResults.Error);

            var movieResult = allResults.Data?.MovieResults.FirstOrDefault();
            if (movieResult is null)
                return Result<GetMovieByImdbResponseDto?>.Failure(new Error("404", $"[{imdbId}] filme inválido"));

            var result = await GetAsync<GetMovieByImdbResponseDto>($"{BaseUrl}/movie/{movieResult.Id}?page=1");
            if (result.IsFailure)
                return Result<GetMovieByImdbResponseDto?>.Failure(result.Error);

            using var client = new HttpClient();
            var images = await GetImagesAsync(client, "movie", movieResult.Id, configuration["Tmdb:Key"]);
            result.Data!.ApplyImages(images ?? new TmdbImagesResponseDto());

            return Result<GetMovieByImdbResponseDto?>.Success(result.Data);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<GetMovieByImdbResponseDto?>.Failure(error);
        }
    }

    public async Task<Result<GetSeriesByImdbResponseDto?>> GetSeriesByImdbIdAsync(string imdbId)
    {
        try
        {
            var allResults = await GetAllResultsByImdbIdAsync(imdbId);
            if (allResults.IsFailure)
                return Result<GetSeriesByImdbResponseDto?>.Failure(allResults.Error);

            var seriesResult = allResults.Data?.TvResults.FirstOrDefault();
            if (seriesResult == null)
                return Result<GetSeriesByImdbResponseDto?>.Failure(
                    new Error("404", $"[{imdbId}] série inválida"));

            var result = await GetAsync<GetSeriesByImdbResponseDto>($"{BaseUrl}/tv/{seriesResult.Id}");
            if (result.IsFailure)
                return Result<GetSeriesByImdbResponseDto?>.Failure(result.Error);

            using var client = new HttpClient();
            var images = await GetImagesAsync(client, "tv", seriesResult.Id, configuration["Tmdb:Key"]);
            result.Data!.ApplyImages(images ?? new TmdbImagesResponseDto());

            return Result<GetSeriesByImdbResponseDto?>.Success(result.Data);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<GetSeriesByImdbResponseDto?>.Failure(error);
        }
    }

    public async Task<Result<GetSeriesByImdbResponseDto?>> GetSeriesByTmdbIdAsync(int tmdbId)
    {
        var cacheKey = $"tmdb:series:details:{tmdbId}";
        var cached = await cacheService.GetValueAsync<GetSeriesByImdbResponseDto>(cacheKey);
        if (cached is not null)
            return Result<GetSeriesByImdbResponseDto?>.Success(cached);

        var result = await GetAsync<GetSeriesByImdbResponseDto>($"{BaseUrl}/tv/{tmdbId}");

        if (result.IsFailure)
            return Result<GetSeriesByImdbResponseDto?>.Failure(result.Error);

        await cacheService.SetValueAsync(cacheKey, SearchCacheTtl, result.Data!);

        return Result<GetSeriesByImdbResponseDto?>.Success(result.Data);
    }

    public async Task<Result<GetSeriesEpisodesBySeasonResponseDto?>> GetSeriesEpisodesBySeasonAsync(string imdbId, int season)
    {
        try
        {
            var seriesResult = await GetSeriesByImdbIdAsync(imdbId);
            if (seriesResult.IsFailure)
                return Result<GetSeriesEpisodesBySeasonResponseDto?>.Failure(seriesResult.Error);

            if (seriesResult.Data is null)
                return Result<GetSeriesEpisodesBySeasonResponseDto?>.Failure(
                    new Error("404", $"[{imdbId}] série inválida"));

            return await GetSeriesEpisodesByTmdbIdAsync(seriesResult.Data.Id, season);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<GetSeriesEpisodesBySeasonResponseDto?>.Failure(error);
        }
    }

    public async Task<Result<IReadOnlyCollection<TmdbSearchResultDto>>> SearchMoviesAsync(string query, int page = 1)
    {
        var normalizedQuery = query.Trim();
        if (string.IsNullOrWhiteSpace(normalizedQuery))
            return Result<IReadOnlyCollection<TmdbSearchResultDto>>.Success([]);

        var cacheKey = $"tmdb:search:movie:{page}:{normalizedQuery.ToLowerInvariant()}";
        var cached = await cacheService.GetValueAsync<List<TmdbSearchResultDto>>(cacheKey);
        if (cached is not null)
            return Result<IReadOnlyCollection<TmdbSearchResultDto>>.Success(cached);

        var result = await GetAsync<TmdbSearchResponseDto<TmdbSearchMovieResultDto>>(
            $"{BaseUrl}/search/movie?query={Uri.EscapeDataString(normalizedQuery)}&page={page}&include_adult=false");

        if (result.IsFailure)
            return Result<IReadOnlyCollection<TmdbSearchResultDto>>.Failure(result.Error);

        var items = result.Data!.Results.Select(TmdbSearchResultDto.FromMovie).ToList();
        await cacheService.SetValueAsync(cacheKey, SearchCacheTtl, items);

        return Result<IReadOnlyCollection<TmdbSearchResultDto>>.Success(items);
    }

    public async Task<Result<IReadOnlyCollection<TmdbSearchResultDto>>> SearchSeriesAsync(string query, int page = 1)
    {
        var normalizedQuery = query.Trim();
        if (string.IsNullOrWhiteSpace(normalizedQuery))
            return Result<IReadOnlyCollection<TmdbSearchResultDto>>.Success([]);

        var cacheKey = $"tmdb:search:series:{page}:{normalizedQuery.ToLowerInvariant()}";
        var cached = await cacheService.GetValueAsync<List<TmdbSearchResultDto>>(cacheKey);
        if (cached is not null)
            return Result<IReadOnlyCollection<TmdbSearchResultDto>>.Success(cached);

        var result = await GetAsync<TmdbSearchResponseDto<TmdbSearchSeriesResultDto>>(
            $"{BaseUrl}/search/tv?query={Uri.EscapeDataString(normalizedQuery)}&page={page}&include_adult=false");

        if (result.IsFailure)
            return Result<IReadOnlyCollection<TmdbSearchResultDto>>.Failure(result.Error);

        var items = result.Data!.Results.Select(TmdbSearchResultDto.FromSeries).ToList();
        await cacheService.SetValueAsync(cacheKey, SearchCacheTtl, items);

        return Result<IReadOnlyCollection<TmdbSearchResultDto>>.Success(items);
    }

    public Task<Result<string?>> GetMovieExternalIdsAsync(int tmdbId)
        => GetExternalIdsAsync("movie", tmdbId);

    public Task<Result<string?>> GetSeriesExternalIdsAsync(int tmdbId)
        => GetExternalIdsAsync("tv", tmdbId);

    public async Task<Result<GetSeriesEpisodesBySeasonResponseDto?>> GetSeriesEpisodesByTmdbIdAsync(int tmdbId, int season)
    {
        var cacheKey = $"tmdb:tv:{tmdbId}:season:{season}";
        var cached = await cacheService.GetValueAsync<GetSeriesEpisodesBySeasonResponseDto>(cacheKey);
        if (cached is not null)
            return Result<GetSeriesEpisodesBySeasonResponseDto?>.Success(cached);

        var result = await GetAsync<GetSeriesEpisodesBySeasonResponseDto>(
            $"{BaseUrl}/tv/{tmdbId}/season/{season}");

        if (result.IsFailure)
            return Result<GetSeriesEpisodesBySeasonResponseDto?>.Failure(result.Error);

        await cacheService.SetValueAsync(cacheKey, EpisodesCacheTtl, result.Data!);

        return Result<GetSeriesEpisodesBySeasonResponseDto?>.Success(result.Data);
    }

    private async Task<Result<string?>> GetExternalIdsAsync(string mediaType, int tmdbId)
    {
        var cacheKey = $"tmdb:{mediaType}:{tmdbId}:external_ids";
        var cached = await cacheService.GetValueAsync<string>(cacheKey);
        if (cached is not null)
            return Result<string?>.Success(cached);

        var result = await GetAsync<TmdbExternalIdsResponseDto>(
            $"{BaseUrl}/{mediaType}/{tmdbId}/external_ids");

        if (result.IsFailure)
            return Result<string?>.Failure(result.Error);

        var imdbId = result.Data!.ImdbId ?? string.Empty;
        await cacheService.SetValueAsync(cacheKey, ExternalIdsCacheTtl, imdbId);

        return Result<string?>.Success(string.IsNullOrWhiteSpace(imdbId) ? null : imdbId);
    }

    private async Task<Result<T?>> GetAsync<T>(string url) where T : class
    {
        try
        {
            using var client = new HttpClient();
            var tmdbKey = configuration["Tmdb:Key"];

            var separator = url.Contains('?') ? "&" : "?";
            var response = await client.GetAsync($"{url}{separator}api_key={tmdbKey}&language=pt-BR");

            if (!response.IsSuccessStatusCode)
                return Result<T?>.Failure(
                    new Error("503", "Não foi possível consultar o TMDB no momento. Tente novamente."));

            var responseJsonString = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<T>(responseJsonString);

            if (result is null)
                return Result<T?>.Failure(
                    new Error("404", "O TMDB não retornou dados para a consulta."));

            return Result<T?>.Success(result);
        }
        catch (Exception ex)
        {
            var error = new Error("503", ex.InnerException?.Message ?? ex.Message);
            return Result<T?>.Failure(error);
        }
    }

    private async Task<TmdbImagesResponseDto?> GetImagesAsync(HttpClient client, string mediaType, int tmdbId, string? tmdbKey)
    {
        var response = await client.GetAsync($"{BaseUrl}/{mediaType}/{tmdbId}/images?api_key={tmdbKey}&include_image_language=pt-BR,null");
        if (!response.IsSuccessStatusCode) return null;

        return JsonConvert.DeserializeObject<TmdbImagesResponseDto>(await response.Content.ReadAsStringAsync());
    }
}
