using XerifeTv.CMS.Modules.Abstractions.Enums;
using XerifeTv.CMS.Modules.Abstractions.ValueObjects;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Integrations.Tmdb.Dtos;
using XerifeTv.CMS.Modules.Integrations.Tmdb.Services;
using XerifeTv.CMS.Modules.People.Dtos.Request;
using XerifeTv.CMS.Modules.People.Dtos.Response;
using XerifeTv.CMS.Modules.People.Interfaces;

namespace XerifeTv.CMS.Modules.People;

public sealed class WorkCreditsImportService(
    ITmdbService tmdbService,
    ITmdbCreditsService tmdbCreditsService,
    IPersonService personService,
    IPersonRepository personRepository,
    ILogger<WorkCreditsImportService> logger) : IWorkCreditsImportService
{
    private const string UnavailableMessage =
        "Não foi possível consultar os créditos no TMDB. Tente novamente em instantes.";

    private const int MaxConcurrentPersonLookups = 4;

    private static readonly HashSet<string> ImportantCrewJobs = new(StringComparer.OrdinalIgnoreCase)
    {
        "Director",
        "Creator",
        "Writer",
        "Screenplay",
        "Story",
        "Teleplay",
        "Producer",
        "Executive Producer",
        "Co-Producer",
        "Line Producer",
        "Associate Producer",
        "Casting",
        "Director of Photography",
        "Editor",
        "Original Music Composer",
        "Music",
        "Production Design",
        "Art Direction",
        "Costume Design"
    };

    public Task<Result<ImportWorkCreditsResponseDto>> ImportMovieCreditsAsync(ImportWorkCreditsRequestDto dto)
        => ImportAsync(dto, isSeries: false);

    public Task<Result<ImportWorkCreditsResponseDto>> ImportSeriesCreditsAsync(ImportWorkCreditsRequestDto dto)
        => ImportAsync(dto, isSeries: true);

    private async Task<Result<ImportWorkCreditsResponseDto>> ImportAsync(
        ImportWorkCreditsRequestDto dto,
        bool isSeries)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.ImdbId))
            {
                return Result<ImportWorkCreditsResponseDto>.Failure(
                    new Error("400", "Preencha o IMDb ID da obra para importar o elenco e a equipe."));
            }

            var imdbId = dto.ImdbId.Trim();
            var findResponse = await tmdbService.GetAllResultsByImdbIdAsync(imdbId);

            if (findResponse.IsFailure)
                return Result<ImportWorkCreditsResponseDto>.Failure(new Error("503", UnavailableMessage));

            var tmdbId = isSeries
                ? findResponse.Data?.TvResults.FirstOrDefault()?.Id ?? 0
                : findResponse.Data?.MovieResults.FirstOrDefault()?.Id ?? 0;

            if (tmdbId <= 0)
            {
                return Result<ImportWorkCreditsResponseDto>.Failure(
                    new Error("404", "Obra não encontrada no TMDB para o IMDb ID informado."));
            }

            var creditsResponse = isSeries
                ? await tmdbCreditsService.GetSeriesCreditsAsync(tmdbId)
                : await tmdbCreditsService.GetMovieCreditsAsync(tmdbId);

            if (creditsResponse.IsFailure)
                return Result<ImportWorkCreditsResponseDto>.Failure(creditsResponse.Error);

            var ignored = 0;
            var candidates = BuildCandidates(creditsResponse.Data!, ref ignored);

            var current = (dto.Credits ?? [])
                .Select(x => x.ToCredit())
                .ToList();

            var resolvedPeople = await ResolvePeopleAsync(candidates);

            var createdPeople = new HashSet<int>();
            var reusedPeople = new HashSet<int>();
            var incoming = new List<Credit>(candidates.Count);

            foreach (var candidate in candidates)
            {
                if (!resolvedPeople.TryGetValue(candidate.TmdbId, out var upsert))
                {
                    ignored++;
                    continue;
                }

                if (upsert.Created) createdPeople.Add(candidate.TmdbId);
                else reusedPeople.Add(candidate.TmdbId);

                candidate.Credit.PersonId = upsert.Person.Id;
                incoming.Add(candidate.Credit);
            }

            var merged = CreditHelper.Merge(current, incoming, out var imported, out var duplicateIgnored);
            ignored += duplicateIgnored;

            var peopleById = await LoadPeopleByIdAsync(merged);

            var response = new ImportWorkCreditsResponseDto
            {
                Credits = merged
                    .Select(credit => GetCreditResponseDto.FromCredit(
                        credit,
                        peopleById.GetValueOrDefault(credit.PersonId)))
                    .ToList(),
                Imported = imported,
                Ignored = ignored,
                PeopleCreated = createdPeople.Count,
                PeopleReused = reusedPeople.Count
            };

            return Result<ImportWorkCreditsResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to import work credits for IMDb id {ImdbId}", dto.ImdbId);
            return Result<ImportWorkCreditsResponseDto>.Failure(new Error("500", UnavailableMessage));
        }
    }

    private async Task<Dictionary<int, PersonUpsertResult>> ResolvePeopleAsync(
        IReadOnlyCollection<CreditCandidate> candidates)
    {
        var distinct = candidates
            .GroupBy(x => x.TmdbId)
            .Select(group => group.First())
            .ToList();

        if (distinct.Count == 0)
            return [];

        using var throttle = new SemaphoreSlim(MaxConcurrentPersonLookups);

        var tasks = distinct.Select(async candidate =>
        {
            await throttle.WaitAsync();

            try
            {
                var response = await personService.EnsureLocalPersonFromTmdbAsync(
                    candidate.TmdbId,
                    candidate.Name,
                    candidate.ProfileUrl);

                return (candidate.TmdbId, Response: response);
            }
            finally
            {
                throttle.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        return results
            .Where(result => result.Response.IsSuccess && result.Response.Data is not null)
            .ToDictionary(result => result.TmdbId, result => result.Response.Data!);
    }

    private static List<CreditCandidate> BuildCandidates(TmdbCreditsResponseDto credits, ref int ignored)
    {
        var candidates = new List<CreditCandidate>(credits.Cast.Count + credits.Crew.Count);

        foreach (var cast in credits.Cast)
        {
            if (cast.Id <= 0 || string.IsNullOrWhiteSpace(cast.Name))
            {
                ignored++;
                continue;
            }

            candidates.Add(new CreditCandidate(
                cast.Id,
                cast.Name.Trim(),
                cast.ProfileUrl,
                new Credit
                {
                    Type = ECreditType.Cast,
                    Character = cast.Character?.Trim() ?? string.Empty,
                    Order = cast.Order
                }));
        }

        foreach (var crew in credits.Crew)
        {
            if (crew.Id <= 0 || string.IsNullOrWhiteSpace(crew.Name) ||
                string.IsNullOrWhiteSpace(crew.Department) || string.IsNullOrWhiteSpace(crew.Job))
            {
                ignored++;
                continue;
            }

            if (!ImportantCrewJobs.Contains(crew.Job.Trim()))
                continue;

            candidates.Add(new CreditCandidate(
                crew.Id,
                crew.Name.Trim(),
                crew.ProfileUrl,
                new Credit
                {
                    Type = ECreditType.Crew,
                    Department = crew.Department.Trim(),
                    Job = crew.Job.Trim()
                }));
        }

        return candidates;
    }

    private async Task<Dictionary<string, PersonEntity>> LoadPeopleByIdAsync(IEnumerable<Credit> credits)
    {
        var ids = credits
            .Select(x => x.PersonId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var people = await personRepository.GetByIdsAsync(ids);

        return people.ToDictionary(x => x.Id, x => x);
    }

    private sealed record CreditCandidate(int TmdbId, string Name, string? ProfileUrl, Credit Credit);
}
