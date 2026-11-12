using MongoDB.Driver;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Integrations.Tmdb.Dtos;
using XerifeTv.CMS.Modules.Integrations.Tmdb.Services;
using XerifeTv.CMS.Modules.People.Dtos.Request;
using XerifeTv.CMS.Modules.People.Dtos.Response;
using XerifeTv.CMS.Modules.People.Interfaces;
using XerifeTv.CMS.Modules.People.Specifications;

namespace XerifeTv.CMS.Modules.People;

public sealed class PersonService(
    IPersonRepository repository,
    ITmdbPersonService tmdbPersonService,
    ILogger<PersonService> logger) : IPersonService
{
    public async Task<Result<GetPersonResponseDto?>> GetAsync(string id)
    {
        try
        {
            var entity = await repository.GetByIdAsync(id);

            if (entity is null)
                return Result<GetPersonResponseDto?>.Failure(new Error("404", "Pessoa não encontrada"));

            return Result<GetPersonResponseDto?>.Success(GetPersonResponseDto.FromEntity(entity));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get person {PersonId}", id);
            return Result<GetPersonResponseDto?>.Failure(
                new Error("500", "Não foi possível carregar os dados da pessoa."));
        }
    }

    public async Task<Result<GetPersonResponseDto>> UpdateAsync(UpdatePersonRequestDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Result<GetPersonResponseDto>.Failure(new Error("400", "Informe o nome da pessoa."));

            var entity = await repository.GetByIdAsync(dto.Id);

            if (entity is null)
                return Result<GetPersonResponseDto>.Failure(new Error("404", "Pessoa não encontrada"));

            var updated = Apply(dto, entity);
            var uniqueTmdbId = new UniqueTmdbIdSpecification(repository);

            if (!await uniqueTmdbId.IsSatisfiedByAsync(updated))
            {
                return Result<GetPersonResponseDto>.Failure(
                    new Error("409", "Já existe uma pessoa cadastrada com este TMDB Person ID."));
            }

            await repository.UpdateAsync(updated);

            return Result<GetPersonResponseDto>.Success(GetPersonResponseDto.FromEntity(updated));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update person {PersonId}", dto.Id);
            return Result<GetPersonResponseDto>.Failure(
                new Error("500", "Não foi possível salvar os dados da pessoa."));
        }
    }

    public async Task<Result<IEnumerable<PersonSearchResultDto>>> SearchTmdbAsync(string name)
    {
        try
        {
            var tmdbResponse = await tmdbPersonService.SearchAsync(name);

            if (tmdbResponse.IsFailure)
                return Result<IEnumerable<PersonSearchResultDto>>.Failure(tmdbResponse.Error);

            var results = tmdbResponse.Data?.Take(20).ToList() ?? [];

            if (results.Count == 0)
                return Result<IEnumerable<PersonSearchResultDto>>.Success([]);

            var registered = await repository.GetByTmdbIdsAsync(results.Select(x => x.Id));
            var registeredByTmdbId = registered
                .Where(x => x.TmdbId.HasValue)
                .ToDictionary(x => x.TmdbId!.Value, x => x);

            var response = results.Select(result =>
            {
                registeredByTmdbId.TryGetValue(result.Id, out var local);

                return new PersonSearchResultDto
                {
                    TmdbId = result.Id,
                    Name = result.Name,
                    ProfileUrl = result.ProfileUrl,
                    LocalId = local?.Id
                };
            });

            return Result<IEnumerable<PersonSearchResultDto>>.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to search people on TMDB");
            return Result<IEnumerable<PersonSearchResultDto>>.Failure(
                new Error("503", "Não foi possível pesquisar pessoas no TMDB. Tente novamente em instantes."));
        }
    }

    public Task<Result<GetPersonResponseDto>> ImportPersonAsync(int tmdbId)
        => UpsertFromTmdbAsync(tmdbId);

    public async Task<Result<GetPersonResponseDto>> RefreshFromTmdbAsync(string id)
    {
        var entity = await repository.GetByIdAsync(id);

        if (entity is null)
            return Result<GetPersonResponseDto>.Failure(new Error("404", "Pessoa não encontrada"));

        if (entity.TmdbId is null)
        {
            return Result<GetPersonResponseDto>.Failure(
                new Error("400", "Esta pessoa não possui TMDB Person ID para enriquecimento."));
        }

        return await UpsertFromTmdbAsync(entity.TmdbId.Value);
    }

    public async Task<Result<PersonUpsertResult>> EnsureLocalPersonFromTmdbAsync(
        int tmdbId,
        string fallbackName,
        string? fallbackProfileUrl)
    {
        try
        {
            var existing = await repository.GetByTmdbIdAsync(tmdbId);

            if (existing is not null && !NeedsEnrichment(existing))
            {
                await NormalizeStoredProfileUrlAsync(existing);
                return Result<PersonUpsertResult>.Success(new PersonUpsertResult(existing, Created: false));
            }

            var details = await GetDetailsOrDefaultAsync(tmdbId);

            if (existing is not null)
            {
                ApplyTmdbDetails(existing, details, fallbackName, fallbackProfileUrl, fillOnlyMissing: true);

                try
                {
                    await repository.UpdateAsync(existing);
                }
                catch (Exception ex)
                {
                    // O enriquecimento é melhor esforço: manter os dados locais já
                    // carregados é preferível a descartar a pessoa da importação.
                    logger.LogWarning(ex, "Failed to enrich local person {TmdbId}", tmdbId);
                }

                return Result<PersonUpsertResult>.Success(new PersonUpsertResult(existing, Created: false));
            }

            var entity = new PersonEntity { TmdbId = tmdbId };
            ApplyTmdbDetails(entity, details, fallbackName, fallbackProfileUrl, fillOnlyMissing: false);

            await repository.CreateAsync(entity);

            return Result<PersonUpsertResult>.Success(new PersonUpsertResult(entity, Created: true));
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            var existing = await repository.GetByTmdbIdAsync(tmdbId);

            if (existing is not null)
                return Result<PersonUpsertResult>.Success(new PersonUpsertResult(existing, Created: false));

            return Result<PersonUpsertResult>.Failure(
                new Error("409", "Não foi possível cadastrar a pessoa por conflito de TMDB Person ID."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to ensure local person {TmdbId}", tmdbId);
            return Result<PersonUpsertResult>.Failure(
                new Error("500", "Não foi possível cadastrar a pessoa local."));
        }
    }

    public async Task<Result<IReadOnlyCollection<PersonEntity>>> GetByTmdbIdsAsync(IEnumerable<int> tmdbIds)
    {
        try
        {
            var response = await repository.GetByTmdbIdsAsync(tmdbIds);
            return Result<IReadOnlyCollection<PersonEntity>>.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get people by TMDB ids");
            return Result<IReadOnlyCollection<PersonEntity>>.Failure(
                new Error("500", "Não foi possível carregar as pessoas."));
        }
    }

    private async Task<TmdbPersonDetailsDto?> GetDetailsOrDefaultAsync(int tmdbId)
    {
        var response = await tmdbPersonService.GetDetailsAsync(tmdbId);

        if (response.IsFailure)
        {
            logger.LogWarning(
                "TMDB person details unavailable for {TmdbId}; falling back to credits data",
                tmdbId);
            return null;
        }

        return response.Data;
    }

    private static bool NeedsEnrichment(PersonEntity person)
        => string.IsNullOrWhiteSpace(person.Biography)
           && person.Birthday is null
           && person.Deathday is null
           && string.IsNullOrWhiteSpace(person.PlaceOfBirth)
           && string.IsNullOrWhiteSpace(person.ImdbId);

    private static void ApplyTmdbDetails(
        PersonEntity entity,
        TmdbPersonDetailsDto? details,
        string fallbackName,
        string? fallbackProfileUrl,
        bool fillOnlyMissing)
    {
        var name = string.IsNullOrWhiteSpace(details?.Name) ? fallbackName : details.Name;

        if (!fillOnlyMissing || string.IsNullOrWhiteSpace(entity.Name))
        {
            if (!string.IsNullOrWhiteSpace(name))
                entity.Name = name.Trim();
        }

        if (!fillOnlyMissing || string.IsNullOrWhiteSpace(entity.Biography))
            entity.Biography = NullIfWhiteSpace(details?.Biography);

        if (!fillOnlyMissing || entity.Birthday is null)
            entity.Birthday = details?.Birthday;

        if (!fillOnlyMissing || entity.Deathday is null)
            entity.Deathday = details?.Deathday;

        if (!fillOnlyMissing || string.IsNullOrWhiteSpace(entity.PlaceOfBirth))
            entity.PlaceOfBirth = NullIfWhiteSpace(details?.PlaceOfBirth);

        if (!fillOnlyMissing || string.IsNullOrWhiteSpace(entity.ImdbId))
            entity.ImdbId = NullIfWhiteSpace(details?.ImdbId);

        if (!fillOnlyMissing || string.IsNullOrWhiteSpace(entity.ProfileUrl))
        {
            entity.ProfileUrl = string.IsNullOrWhiteSpace(details?.ProfileUrl)
                ? NullIfWhiteSpace(fallbackProfileUrl)
                : details.ProfileUrl;
        }

        entity.ProfileUrl = TmdbImageUrl.NormalizeStoredUrl(entity.ProfileUrl);
    }

    private async Task NormalizeStoredProfileUrlAsync(PersonEntity person)
    {
        var normalized = TmdbImageUrl.NormalizeStoredUrl(person.ProfileUrl);

        if (string.Equals(normalized, person.ProfileUrl, StringComparison.Ordinal))
            return;

        person.ProfileUrl = normalized;

        try
        {
            await repository.UpdateAsync(person);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to normalize profile image for person {TmdbId}", person.TmdbId);
        }
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<Result<GetPersonResponseDto>> UpsertFromTmdbAsync(int tmdbId)
    {
        try
        {
            var detailsResponse = await tmdbPersonService.GetDetailsAsync(tmdbId);

            if (detailsResponse.IsFailure)
                return Result<GetPersonResponseDto>.Failure(detailsResponse.Error);

            var details = detailsResponse.Data!;
            var entity = await repository.GetByTmdbIdAsync(tmdbId);
            var isNew = entity is null;
            entity ??= new PersonEntity { TmdbId = tmdbId };

            entity.Name = string.IsNullOrWhiteSpace(details.Name) ? entity.Name : details.Name;
            entity.Biography = details.Biography;
            entity.Birthday = details.Birthday;
            entity.Deathday = details.Deathday;
            entity.PlaceOfBirth = details.PlaceOfBirth;
            entity.ProfileUrl = details.ProfileUrl;
            entity.ImdbId = details.ImdbId;

            if (isNew) await repository.CreateAsync(entity);
            else await repository.UpdateAsync(entity);

            return Result<GetPersonResponseDto>.Success(GetPersonResponseDto.FromEntity(entity));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to import person {TmdbId} from TMDB", tmdbId);
            return Result<GetPersonResponseDto>.Failure(
                new Error("500", "Não foi possível importar os dados da pessoa."));
        }
    }

    private static PersonEntity Apply(UpdatePersonRequestDto dto, PersonEntity entity)
    {
        entity.TmdbId = dto.TmdbId;
        entity.ImdbId = string.IsNullOrWhiteSpace(dto.ImdbId) ? null : dto.ImdbId.Trim();
        entity.Name = dto.Name.Trim();
        entity.Biography = string.IsNullOrWhiteSpace(dto.Biography) ? null : dto.Biography.Trim();
        entity.Birthday = dto.Birthday;
        entity.Deathday = dto.Deathday;
        entity.PlaceOfBirth = string.IsNullOrWhiteSpace(dto.PlaceOfBirth) ? null : dto.PlaceOfBirth.Trim();
        entity.ProfileUrl = string.IsNullOrWhiteSpace(dto.ProfileUrl) ? null : dto.ProfileUrl.Trim();

        return entity;
    }
}
