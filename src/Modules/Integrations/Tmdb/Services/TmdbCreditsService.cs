using System.Net;
using Newtonsoft.Json;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Integrations.Tmdb.Dtos;

namespace XerifeTv.CMS.Modules.Integrations.Tmdb.Services;

public sealed class TmdbCreditsService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<TmdbCreditsService> logger) : ITmdbCreditsService
{
    private const string BaseUrl = "https://api.themoviedb.org/3";
    private const string UnavailableMessage = "Não foi possível consultar os créditos no TMDB. Tente novamente em instantes.";

    public Task<Result<TmdbCreditsResponseDto>> GetMovieCreditsAsync(int tmdbId)
        => GetCreditsAsync("movie", tmdbId);

    public Task<Result<TmdbCreditsResponseDto>> GetSeriesCreditsAsync(int tmdbId)
        => GetCreditsAsync("tv", tmdbId);

    private async Task<Result<TmdbCreditsResponseDto>> GetCreditsAsync(string mediaType, int tmdbId)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            var tmdbKey = configuration["Tmdb:Key"];
            var url = $"{BaseUrl}/{mediaType}/{tmdbId}/credits?api_key={tmdbKey}&language=pt-BR";

            var response = await client.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return Result<TmdbCreditsResponseDto>.Failure(
                    new Error("404", "Créditos não encontrados no TMDB"));

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("TMDB credits failed with status {StatusCode}", (int)response.StatusCode);
                return Result<TmdbCreditsResponseDto>.Failure(new Error("503", UnavailableMessage));
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<TmdbCreditsResponseDto>(json);

            if (result is null)
                return Result<TmdbCreditsResponseDto>.Failure(
                    new Error("404", "Créditos não encontrados no TMDB"));

            return Result<TmdbCreditsResponseDto>.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "TMDB credits failed");
            return Result<TmdbCreditsResponseDto>.Failure(new Error("503", UnavailableMessage));
        }
    }
}
