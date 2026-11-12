using Newtonsoft.Json;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Integrations.Tmdb.Dtos;

namespace XerifeTv.CMS.Modules.Integrations.Tmdb.Services;

public class TmdbService(IConfiguration configuration) : ITmdbService
{
    public async Task<Result<GetAllResultsByImdbIdResponseDto?>> GetAllResultsByImdbIdAsync(string imdbId)
    {
        try
        {
            var client = new HttpClient();
            var url = $"https://api.themoviedb.org/3/find/{imdbId}";
            var tmdbKey = configuration["Tmdb:Key"];

            var response = await client.GetAsync($"{url}?api_key={tmdbKey}&external_source=imdb_id&language=pt-BR");

            if (!response.IsSuccessStatusCode)
                return Result<GetAllResultsByImdbIdResponseDto?>.Failure(
                    new Error("400", $"[{imdbId}] TMDB API retornou: {response.ReasonPhrase}"));

            var responseJsonString = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<GetAllResultsByImdbIdResponseDto>(responseJsonString);

            if (result is null)
                return Result<GetAllResultsByImdbIdResponseDto?>.Failure(
                    new Error("404", $"Imdb ID: {imdbId} inválido"));

            return Result<GetAllResultsByImdbIdResponseDto?>.Success(result);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<GetAllResultsByImdbIdResponseDto?>.Failure(error);
        }
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

            var client = new HttpClient();
            var url = $"https://api.themoviedb.org/3/movie/{movieResult.Id}";
            var tmdbKey = configuration["Tmdb:Key"];

            var response = await client.GetAsync($"{url}?api_key={tmdbKey}&language=pt-BR&page=1");

            if (!response.IsSuccessStatusCode)
                return Result<GetMovieByImdbResponseDto?>.Failure(
                    new Error("400", $"[{imdbId}] TMDB API retornou: {response.ReasonPhrase}"));

            var responseJsonString = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<GetMovieByImdbResponseDto>(responseJsonString);

            if (result is null)
                return Result<GetMovieByImdbResponseDto?>.Failure(
                    new Error("404", $"Imdb ID: {imdbId} inválido"));

            var images = await GetImagesAsync(client, "movie", movieResult.Id, tmdbKey);
            if (images is not null) result.ApplyImages(images);
            else result.ApplyImages(new TmdbImagesResponseDto());

            return Result<GetMovieByImdbResponseDto?>.Success(result);
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

            var client = new HttpClient();
            var url = $"https://api.themoviedb.org/3/tv/{seriesResult.Id}";
            var tmdbKey = configuration["Tmdb:Key"];

            var response = await client.GetAsync($"{url}?api_key={tmdbKey}&language=pt-BR");

            if (!response.IsSuccessStatusCode)
                return Result<GetSeriesByImdbResponseDto?>.Failure(
                    new Error("400", $"[{imdbId}] TMDB API retornou: {response.ReasonPhrase}"));

            var responseJsonString = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<GetSeriesByImdbResponseDto>(responseJsonString);

            if (result is null)
                return Result<GetSeriesByImdbResponseDto?>.Failure(
                    new Error("404", $"Imdb ID: {imdbId} inválido"));

            var images = await GetImagesAsync(client, "tv", seriesResult.Id, tmdbKey);
            if (images is not null) result.ApplyImages(images);
            else result.ApplyImages(new TmdbImagesResponseDto());

            return Result<GetSeriesByImdbResponseDto?>.Success(result);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<GetSeriesByImdbResponseDto?>.Failure(error);
        }
    }

    public async Task<Result<GetSeriesEpisodesBySeasonResponseDto?>> GetSeriesEpisodesBySeasonAsync(string imdbId, int season)
    {
        try
        {
            var seriesResult = await GetSeriesByImdbIdAsync(imdbId);
            if (seriesResult.IsFailure)
                return Result<GetSeriesEpisodesBySeasonResponseDto?>.Failure(seriesResult.Error);

            var client = new HttpClient();
            var url = $"https://api.themoviedb.org/3/tv/{seriesResult.Data?.Id}/season/{season}";
            var tmdbKey = configuration["Tmdb:Key"];

            var response = await client.GetAsync($"{url}?api_key={tmdbKey}&language=pt-BR");

            if (!response.IsSuccessStatusCode)
                return Result<GetSeriesEpisodesBySeasonResponseDto?>.Failure(
                    new Error("400", $"[{imdbId}] TMDB API retornou: {response.ReasonPhrase}"));

            var responseJsonString = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<GetSeriesEpisodesBySeasonResponseDto>(responseJsonString);

            if (result is null)
                return Result<GetSeriesEpisodesBySeasonResponseDto?>.Failure(
                    new Error("404", $"Imdb ID: {imdbId} inválido"));

            return Result<GetSeriesEpisodesBySeasonResponseDto?>.Success(result);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<GetSeriesEpisodesBySeasonResponseDto?>.Failure(error);
        }
    }

    private async Task<TmdbImagesResponseDto?> GetImagesAsync(HttpClient client, string mediaType, int tmdbId, string? tmdbKey)
    {
        var response = await client.GetAsync($"https://api.themoviedb.org/3/{mediaType}/{tmdbId}/images?api_key={tmdbKey}&include_image_language=pt-BR,null");
        if (!response.IsSuccessStatusCode) return null;

        return JsonConvert.DeserializeObject<TmdbImagesResponseDto>(await response.Content.ReadAsStringAsync());
    }
}

