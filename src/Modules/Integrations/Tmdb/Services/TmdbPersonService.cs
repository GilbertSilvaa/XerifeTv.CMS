using System.Globalization;
using System.Net;
using Newtonsoft.Json;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Integrations.Tmdb.Dtos;

namespace XerifeTv.CMS.Modules.Integrations.Tmdb.Services;

public sealed class TmdbPersonService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<TmdbPersonService> logger) : ITmdbPersonService
{
    private const string BaseUrl = "https://api.themoviedb.org/3";
    private const string UnavailableMessage = "Não foi possível consultar o TMDB. Tente novamente em instantes.";

    public async Task<Result<IEnumerable<TmdbPersonSearchResultDto>>> SearchAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<IEnumerable<TmdbPersonSearchResultDto>>.Success([]);

        try
        {
            var client = httpClientFactory.CreateClient();
            var tmdbKey = configuration["Tmdb:Key"];
            var url = $"{BaseUrl}/search/person?api_key={tmdbKey}&language=pt-BR&query={Uri.EscapeDataString(name.Trim())}";

            var response = await client.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return Result<IEnumerable<TmdbPersonSearchResultDto>>.Failure(
                    new Error("404", "Pessoa não encontrada no TMDB"));

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("TMDB person search failed with status {StatusCode}", (int)response.StatusCode);
                return Result<IEnumerable<TmdbPersonSearchResultDto>>.Failure(new Error("503", UnavailableMessage));
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<TmdbPersonSearchResponseDto>(json);

            return Result<IEnumerable<TmdbPersonSearchResultDto>>.Success(result?.Results ?? []);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "TMDB person search failed");
            return Result<IEnumerable<TmdbPersonSearchResultDto>>.Failure(new Error("503", UnavailableMessage));
        }
    }

    public async Task<Result<TmdbPersonDetailsDto>> GetDetailsAsync(int tmdbId)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            var tmdbKey = configuration["Tmdb:Key"];
            var url = $"{BaseUrl}/person/{tmdbId}?api_key={tmdbKey}&language=pt-BR";

            var response = await client.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return Result<TmdbPersonDetailsDto>.Failure(
                    new Error("404", "Pessoa não encontrada no TMDB"));

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("TMDB person details failed with status {StatusCode}", (int)response.StatusCode);
                return Result<TmdbPersonDetailsDto>.Failure(new Error("503", UnavailableMessage));
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<TmdbPersonDetailsResponseDto>(json);

            if (result is null || result.Id <= 0)
                return Result<TmdbPersonDetailsDto>.Failure(new Error("404", "Pessoa não encontrada no TMDB"));

            return Result<TmdbPersonDetailsDto>.Success(new TmdbPersonDetailsDto
            {
                Id = result.Id,
                Name = result.Name,
                Biography = result.Biography,
                Birthday = ParseDate(result.Birthday),
                Deathday = ParseDate(result.Deathday),
                PlaceOfBirth = result.PlaceOfBirth,
                ProfileUrl = result.ProfileUrl,
                ImdbId = result.ImdbId
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "TMDB person details failed");
            return Result<TmdbPersonDetailsDto>.Failure(new Error("503", UnavailableMessage));
        }
    }

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        return DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : null;
    }
}
