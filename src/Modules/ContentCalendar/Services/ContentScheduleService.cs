using MongoDB.Driver;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.ContentCalendar.Dtos.Request;
using XerifeTv.CMS.Modules.ContentCalendar.Dtos.Response;
using XerifeTv.CMS.Modules.ContentCalendar.Entities;
using XerifeTv.CMS.Modules.ContentCalendar.Enums;
using XerifeTv.CMS.Modules.ContentCalendar.Interfaces;
using XerifeTv.CMS.Modules.ContentCalendar.Services;
using XerifeTv.CMS.Modules.Integrations.Tmdb.Services;
using XerifeTv.CMS.Modules.Movie.Interfaces;
using XerifeTv.CMS.Modules.Series.Interfaces;

namespace XerifeTv.CMS.Modules.ContentCalendar;

public sealed class ContentScheduleService(
    IContentScheduleRepository repository,
    ITmdbService tmdbService,
    IMovieRepository movieRepository,
    ISeriesRepository seriesRepository,
    IBusinessClock clock,
    ILogger<ContentScheduleService> logger) : IContentScheduleService
{
    public async Task<Result<GetContentScheduleResponseDto>> CreateAsync(CreateContentScheduleRequestDto dto)
    {
        var validationError = Validate(dto);
        if (validationError is not null)
            return Result<GetContentScheduleResponseDto>.Failure(validationError);

        var isEpisode = dto.ContentType == EContentScheduleType.EPISODE;
        var seasonNumber = isEpisode ? dto.SeasonNumber : null;
        var episodeNumber = isEpisode ? dto.EpisodeNumber : null;

        try
        {
            if (await repository.ExistsByExternalKeyAsync(
                    dto.ContentType, dto.TmdbId, seasonNumber, episodeNumber))
                return Result<GetContentScheduleResponseDto>.Failure(
                    new Error("409", "Já existe um agendamento para este conteúdo."));

            var imdbId = await ResolveImdbIdAsync(dto.ContentType, dto.TmdbId);

            var entity = new ContentScheduleEntity
            {
                ContentType = dto.ContentType,
                TmdbId = dto.TmdbId,
                Title = dto.Title.Trim(),
                ImageUrl = dto.ImageUrl,
                ScheduledDate = dto.ScheduledDate,
                Origin = EScheduleOrigin.MANUAL,
                Status = EScheduleStatus.SCHEDULED,
                SeasonNumber = seasonNumber,
                EpisodeNumber = episodeNumber,
                ImdbId = imdbId
            };

            await repository.CreateAsync(entity);

            return Result<GetContentScheduleResponseDto>.Success(
                GetContentScheduleResponseDto.FromEntity(entity, clock.Today));
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Code == 11000)
        {
            return Result<GetContentScheduleResponseDto>.Failure(
                new Error("409", "Já existe um agendamento para este conteúdo."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create content schedule");
            return Result<GetContentScheduleResponseDto>.Failure(
                new Error("500", ex.InnerException?.Message ?? ex.Message));
        }
    }

    public async Task<Result<IReadOnlyCollection<GetContentScheduleResponseDto>>> GetByPeriodAsync(
        DateOnly start, DateOnly end)
    {
        try
        {
            var schedules = await repository.GetByPeriodAsync(start, end);
            var today = clock.Today;

            return Result<IReadOnlyCollection<GetContentScheduleResponseDto>>.Success(
                [.. schedules.Select(s => GetContentScheduleResponseDto.FromEntity(s, today))]);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list content schedules between {Start} and {End}", start, end);
            return Result<IReadOnlyCollection<GetContentScheduleResponseDto>>.Failure(
                new Error("500", ex.InnerException?.Message ?? ex.Message));
        }
    }

    public async Task<Result<ContentScheduleDetailsResponseDto>> GetDetailsAsync(string id)
    {
        try
        {
            var entity = await repository.GetAsync(id);
            if (entity is null)
                return Result<ContentScheduleDetailsResponseDto>.Failure(
                    new Error("404", "Agendamento não encontrado."));

            var diagnostic = await BuildDiagnosticAsync(entity);

            // Auto-complete when the matching content already exists and is enabled.
            if (diagnostic.Status == EContentScheduleDiagnosticStatus.FOUND &&
                diagnostic.InternalContentId is not null &&
                (entity.Status != EScheduleStatus.REGISTERED || entity.RegisteredContentId is null))
            {
                entity.Complete(diagnostic.InternalContentId, DateTime.UtcNow);
                await repository.UpdateAsync(entity);

                diagnostic = new ContentScheduleDiagnosticDto
                {
                    Status = EContentScheduleDiagnosticStatus.FOUND,
                    Message = "Conteúdo cadastrado.",
                    InternalContentId = entity.RegisteredContentId,
                    CanLinkManually = false
                };
            }

            var schedule = GetContentScheduleResponseDto.FromEntity(entity, clock.Today);

            return Result<ContentScheduleDetailsResponseDto>.Success(
                new ContentScheduleDetailsResponseDto { Schedule = schedule, Diagnostic = diagnostic });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load content schedule details for {ScheduleId}", id);
            return Result<ContentScheduleDetailsResponseDto>.Failure(
                new Error("500", ex.InnerException?.Message ?? ex.Message));
        }
    }

    public async Task<Result<GetContentScheduleResponseDto>> UpdateDateAsync(
        UpdateContentScheduleDateRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Id))
            return Result<GetContentScheduleResponseDto>.Failure(
                new Error("400", "Agendamento inválido."));

        if (dto.ScheduledDate == default)
            return Result<GetContentScheduleResponseDto>.Failure(
                new Error("400", "Informe a data prevista."));

        try
        {
            var entity = await repository.GetAsync(dto.Id);
            if (entity is null)
                return Result<GetContentScheduleResponseDto>.Failure(
                    new Error("404", "Agendamento não encontrado."));

            if (entity.Status == EScheduleStatus.REGISTERED || entity.RegisteredContentId is not null)
                return Result<GetContentScheduleResponseDto>.Failure(
                    new Error("409", "A data de um agendamento já cadastrado não pode ser alterada."));

            var updated = await repository.UpdateScheduledDateAsync(entity.Id, dto.ScheduledDate);
            if (!updated)
                return Result<GetContentScheduleResponseDto>.Failure(
                    new Error("409", "Não foi possível alterar a data do agendamento."));

            entity.ScheduledDate = dto.ScheduledDate;

            return Result<GetContentScheduleResponseDto>.Success(
                GetContentScheduleResponseDto.FromEntity(entity, clock.Today));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update content schedule date for {ScheduleId}", dto.Id);
            return Result<GetContentScheduleResponseDto>.Failure(
                new Error("500", ex.InnerException?.Message ?? ex.Message));
        }
    }

    public async Task<Result<bool>> DeleteAsync(string id)
    {
        try
        {
            var entity = await repository.GetAsync(id);
            if (entity is null)
                return Result<bool>.Failure(new Error("404", "Agendamento não encontrado."));

            await repository.DeleteAsync(id);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete content schedule {ScheduleId}", id);
            return Result<bool>.Failure(new Error("500", ex.InnerException?.Message ?? ex.Message));
        }
    }

    public async Task<Result<ContentScheduleShortcutDto>> GetShortcutAsync(string id)
    {
        try
        {
            var entity = await repository.GetAsync(id);
            if (entity is null)
                return Result<ContentScheduleShortcutDto>.Failure(
                    new Error("404", "Agendamento não encontrado."));

            return entity.ContentType switch
            {
                EContentScheduleType.MOVIE => Result<ContentScheduleShortcutDto>.Success(
                    await BuildMovieShortcutAsync(entity)),
                EContentScheduleType.SERIES => Result<ContentScheduleShortcutDto>.Success(
                    await BuildSeriesShortcutAsync(entity)),
                EContentScheduleType.EPISODE => Result<ContentScheduleShortcutDto>.Success(
                    await BuildEpisodeShortcutAsync(entity)),
                _ => Result<ContentScheduleShortcutDto>.Failure(
                    new Error("400", "Tipo de conteúdo inválido."))
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to resolve content schedule shortcut for {ScheduleId}", id);
            return Result<ContentScheduleShortcutDto>.Failure(
                new Error("500", ex.InnerException?.Message ?? ex.Message));
        }
    }

    public async Task<Result<ContentScheduleDetailsResponseDto>> LinkAsync(LinkContentScheduleRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ContentId))
            return Result<ContentScheduleDetailsResponseDto>.Failure(
                new Error("400", "Informe o conteúdo a vincular."));

        try
        {
            var entity = await repository.GetAsync(dto.Id);
            if (entity is null)
                return Result<ContentScheduleDetailsResponseDto>.Failure(
                    new Error("404", "Agendamento não encontrado."));

            if (entity.Status == EScheduleStatus.REGISTERED || entity.RegisteredContentId is not null)
                return Result<ContentScheduleDetailsResponseDto>.Failure(
                    new Error("409", "Este agendamento já está concluído."));

            var validation = await ValidateLinkTargetAsync(entity, dto);
            if (validation.IsFailure)
                return Result<ContentScheduleDetailsResponseDto>.Failure(validation.Error);

            entity.Complete(dto.ContentId, DateTime.UtcNow);
            await repository.UpdateAsync(entity);

            var schedule = GetContentScheduleResponseDto.FromEntity(entity, clock.Today);
            var diagnostic = await BuildDiagnosticAsync(entity);

            return Result<ContentScheduleDetailsResponseDto>.Success(
                new ContentScheduleDetailsResponseDto { Schedule = schedule, Diagnostic = diagnostic });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to link content schedule {ScheduleId}", dto.Id);
            return Result<ContentScheduleDetailsResponseDto>.Failure(
                new Error("500", ex.InnerException?.Message ?? ex.Message));
        }
    }

    public async Task<Result<GetContentScheduleResponseDto?>> UpsertAutomaticEpisodeAsync(
        AutomaticEpisodeScheduleRequestDto dto)
    {
        if (dto.SeriesTmdbId <= 0 || dto.EpisodeNumber <= 0 || dto.AirDate == default)
            return Result<GetContentScheduleResponseDto?>.Success(null);

        if (dto.AirDate <= clock.Today)
            return Result<GetContentScheduleResponseDto?>.Success(null);

        try
        {
            var existing = await repository.GetByExternalKeyAsync(
                EContentScheduleType.EPISODE, dto.SeriesTmdbId, dto.SeasonNumber, dto.EpisodeNumber);

            if (existing is not null)
            {
                if (existing.Status != EScheduleStatus.REGISTERED && existing.ScheduledDate != dto.AirDate)
                {
                    await repository.UpdateScheduledDateAsync(existing.Id, dto.AirDate);
                    existing.ScheduledDate = dto.AirDate;
                }

                // Keep the title informative even for previously created schedules.
                var desiredTitle = string.IsNullOrWhiteSpace(dto.EpisodeTitle)
                    ? $"{dto.SeriesTitle} • S{dto.SeasonNumber:00}E{dto.EpisodeNumber:00}"
                    : $"{dto.SeriesTitle} • {dto.EpisodeTitle}";

                if (existing.Status != EScheduleStatus.REGISTERED && existing.Title != desiredTitle)
                {
                    existing.Title = desiredTitle;
                    await repository.UpdateAsync(existing);
                }

                return Result<GetContentScheduleResponseDto?>.Success(
                    GetContentScheduleResponseDto.FromEntity(existing, clock.Today));
            }

            if (!dto.AllowCreate)
                return Result<GetContentScheduleResponseDto?>.Success(null);

            var imdbId = dto.SeriesImdbId;
            if (string.IsNullOrWhiteSpace(imdbId))
                imdbId = await ResolveImdbIdAsync(EContentScheduleType.SERIES, dto.SeriesTmdbId);

            var entity = new ContentScheduleEntity
            {
                ContentType = EContentScheduleType.EPISODE,
                TmdbId = dto.SeriesTmdbId,
                Title = string.IsNullOrWhiteSpace(dto.EpisodeTitle)
                    ? $"{dto.SeriesTitle} • S{dto.SeasonNumber:00}E{dto.EpisodeNumber:00}"
                    : $"{dto.SeriesTitle} • {dto.EpisodeTitle}",
                ImageUrl = dto.ImageUrl,
                ScheduledDate = dto.AirDate,
                Origin = EScheduleOrigin.AUTOMATIC_IMPORT,
                Status = EScheduleStatus.SCHEDULED,
                SeasonNumber = dto.SeasonNumber,
                EpisodeNumber = dto.EpisodeNumber,
                ImdbId = imdbId
            };

            await repository.CreateAsync(entity);

            return Result<GetContentScheduleResponseDto?>.Success(
                GetContentScheduleResponseDto.FromEntity(entity, clock.Today));
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Code == 11000)
        {
            var existing = await repository.GetByExternalKeyAsync(
                EContentScheduleType.EPISODE, dto.SeriesTmdbId, dto.SeasonNumber, dto.EpisodeNumber);

            return Result<GetContentScheduleResponseDto?>.Success(
                existing is null
                    ? null
                    : GetContentScheduleResponseDto.FromEntity(existing, clock.Today));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upsert automatic episode schedule");
            return Result<GetContentScheduleResponseDto?>.Failure(
                new Error("500", ex.InnerException?.Message ?? ex.Message));
        }
    }

    public async Task<Result<IReadOnlyCollection<GetContentScheduleResponseDto>>> GetPendingAsync(int limit)
    {
        try
        {
            var today = clock.Today;
            var schedules = await repository.GetPendingAsync(today, limit);

            return Result<IReadOnlyCollection<GetContentScheduleResponseDto>>.Success(
                [.. schedules.Select(s => GetContentScheduleResponseDto.FromEntity(s, today))]);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list pending content schedules");
            return Result<IReadOnlyCollection<GetContentScheduleResponseDto>>.Failure(
                new Error("500", ex.InnerException?.Message ?? ex.Message));
        }
    }

    private async Task<Result<bool>> ValidateLinkTargetAsync(
        ContentScheduleEntity schedule, LinkContentScheduleRequestDto dto)
    {
        switch (schedule.ContentType)
        {
            case EContentScheduleType.MOVIE:
            {
                var movie = await movieRepository.GetAsync(dto.ContentId);
                if (movie is null)
                    return Result<bool>.Failure(new Error("404", "Filme não encontrado."));
                if (movie.Disabled)
                    return Result<bool>.Failure(new Error("409", "O filme está desabilitado."));
                break;
            }

            case EContentScheduleType.SERIES:
            {
                var series = await seriesRepository.GetAsync(dto.ContentId);
                if (series is null)
                    return Result<bool>.Failure(new Error("404", "Série não encontrada."));
                if (series.Disabled)
                    return Result<bool>.Failure(new Error("409", "A série está desabilitada."));
                break;
            }

            case EContentScheduleType.EPISODE:
            {
                if (string.IsNullOrWhiteSpace(dto.SeriesId))
                    return Result<bool>.Failure(
                        new Error("400", "Informe a série do episódio a vincular."));

                var series = await seriesRepository.GetAsync(dto.SeriesId);
                if (series is null)
                    return Result<bool>.Failure(new Error("404", "Série não encontrada."));
                if (series.Disabled)
                    return Result<bool>.Failure(new Error("409", "A série está desabilitada."));

                var episode = await seriesRepository
                    .GetEpisodeBySeriesIdAndEpisodeIdAsync(dto.SeriesId, dto.ContentId);

                if (episode is null)
                    return Result<bool>.Failure(new Error("404", "Episódio não encontrado."));
                if (episode.Disabled)
                    return Result<bool>.Failure(new Error("409", "O episódio está desabilitado."));
                break;
            }

            default:
                return Result<bool>.Failure(new Error("400", "Tipo de conteúdo inválido."));
        }

        return Result<bool>.Success(true);
    }

    private async Task<ContentScheduleDiagnosticDto> BuildDiagnosticAsync(ContentScheduleEntity schedule)
    {
        if (schedule.Status == EScheduleStatus.REGISTERED || schedule.RegisteredContentId is not null)
            return new ContentScheduleDiagnosticDto
            {
                Status = EContentScheduleDiagnosticStatus.FOUND,
                Message = "Conteúdo cadastrado.",
                InternalContentId = schedule.RegisteredContentId,
                CanLinkManually = false
            };

        if (string.IsNullOrWhiteSpace(schedule.ImdbId))
            return new ContentScheduleDiagnosticDto
            {
                Status = EContentScheduleDiagnosticStatus.NOT_FOUND,
                Message = "Não foi possível identificar o IMDb ID deste agendamento no TMDB. Use o vínculo manual.",
                CanLinkManually = true
            };

        switch (schedule.ContentType)
        {
            case EContentScheduleType.MOVIE:
            {
                var movie = await movieRepository.GetByImdbIdAsync(schedule.ImdbId);

                if (movie is null)
                    return NotFound("Conteúdo não encontrado no CMS.");

                if (movie.Disabled)
                    return FoundDisabled("Conteúdo encontrado, mas está desabilitado.", movie.Id);

                return Found("Conteúdo encontrado e habilitado.", movie.Id);
            }

            case EContentScheduleType.SERIES:
            {
                var series = await seriesRepository.GetByImdbIdAsync(schedule.ImdbId);

                if (series is null)
                    return NotFound("Conteúdo não encontrado no CMS.");

                if (series.Disabled)
                    return FoundDisabled("Conteúdo encontrado, mas está desabilitado.", series.Id);

                return Found("Conteúdo encontrado e habilitado.", series.Id);
            }

            case EContentScheduleType.EPISODE:
            {
                var series = await seriesRepository.GetByImdbIdAsync(schedule.ImdbId);

                if (series is null)
                    return NotFound("Série não encontrada no CMS. Cadastre a série antes do episódio.");

                var season = schedule.SeasonNumber ?? 0;
                var number = schedule.EpisodeNumber ?? 0;
                var episode = series.Episodes.FirstOrDefault(e => e.Season == season && e.Number == number);

                if (episode is null)
                    return new ContentScheduleDiagnosticDto
                    {
                        Status = EContentScheduleDiagnosticStatus.SERIES_FOUND_EPISODE_MISSING,
                        Message = $"Série encontrada, episódio T{season:00}E{number:00} não encontrado.",
                        CanLinkManually = true
                    };

                if (episode.Disabled)
                    return FoundDisabled("Episódio encontrado, mas está desabilitado.", episode.Id);

                return Found("Episódio encontrado e habilitado.", episode.Id);
            }

            default:
                return NotFound("Tipo de conteúdo inválido.");
        }

        ContentScheduleDiagnosticDto NotFound(string message) => new()
        {
            Status = EContentScheduleDiagnosticStatus.NOT_FOUND,
            Message = message,
            CanLinkManually = true
        };

        static ContentScheduleDiagnosticDto FoundDisabled(string message, string contentId) => new()
        {
            Status = EContentScheduleDiagnosticStatus.FOUND_DISABLED,
            Message = message,
            InternalContentId = contentId,
            CanLinkManually = false
        };

        static ContentScheduleDiagnosticDto Found(string message, string contentId) => new()
        {
            Status = EContentScheduleDiagnosticStatus.FOUND,
            Message = message,
            InternalContentId = contentId,
            CanLinkManually = false
        };
    }

    private async Task<ContentScheduleShortcutDto> BuildMovieShortcutAsync(ContentScheduleEntity entity)
    {
        if (entity.RegisteredContentId is not null)
            return new ContentScheduleShortcutDto($"/Movies/Form?id={entity.RegisteredContentId}");

        if (!string.IsNullOrWhiteSpace(entity.ImdbId))
        {
            var movie = await movieRepository.GetByImdbIdAsync(entity.ImdbId);
            if (movie is not null)
                return new ContentScheduleShortcutDto($"/Movies/Form?id={movie.Id}");
        }

        if (!string.IsNullOrWhiteSpace(entity.ImdbId))
            return new ContentScheduleShortcutDto($"/Movies/Form?imdbId={entity.ImdbId}");

        return new ContentScheduleShortcutDto(
            "/Movies/Form",
            "IMDb ID não identificado. Pesquise o filme manualmente no formulário.");
    }

    private async Task<ContentScheduleShortcutDto> BuildSeriesShortcutAsync(ContentScheduleEntity entity)
    {
        if (entity.RegisteredContentId is not null)
            return new ContentScheduleShortcutDto($"/Series/Form?id={entity.RegisteredContentId}");

        if (!string.IsNullOrWhiteSpace(entity.ImdbId))
        {
            var series = await seriesRepository.GetByImdbIdAsync(entity.ImdbId);
            if (series is not null)
                return new ContentScheduleShortcutDto($"/Series/Form?id={series.Id}");
        }

        if (!string.IsNullOrWhiteSpace(entity.ImdbId))
            return new ContentScheduleShortcutDto($"/Series/Form?imdbId={entity.ImdbId}");

        return new ContentScheduleShortcutDto(
            "/Series/Form",
            "IMDb ID não identificado. Pesquise a série manualmente no formulário.");
    }

    private async Task<ContentScheduleShortcutDto> BuildEpisodeShortcutAsync(ContentScheduleEntity entity)
    {
        var series = !string.IsNullOrWhiteSpace(entity.ImdbId)
            ? await seriesRepository.GetByImdbIdAsync(entity.ImdbId)
            : null;

        if (series is null && !string.IsNullOrWhiteSpace(entity.RegisteredContentId))
            series = await seriesRepository.GetByEpisodeIdAsync(entity.RegisteredContentId);

        if (series is null)
        {
            return new ContentScheduleShortcutDto(
                string.IsNullOrWhiteSpace(entity.ImdbId)
                    ? "/Series"
                    : $"/Series/Form?imdbId={entity.ImdbId}",
                "A série precisa ser cadastrada antes do episódio.");
        }

        var episode = !string.IsNullOrWhiteSpace(entity.RegisteredContentId)
            ? series.Episodes.FirstOrDefault(e => e.Id == entity.RegisteredContentId)
            : null;

        episode ??= series.Episodes.FirstOrDefault(e =>
            e.Season == (entity.SeasonNumber ?? 0) &&
            e.Number == (entity.EpisodeNumber ?? 0));

        if (episode is not null)
        {
            return new ContentScheduleShortcutDto(
                $"/Series/Episodes?id={series.Id}&season={episode.Season}&episode={episode.Number}&episodeId={episode.Id}");
        }

        var season = entity.SeasonNumber ?? 0;
        var number = entity.EpisodeNumber ?? 0;

        return new ContentScheduleShortcutDto(
            $"/Series/Episodes?id={series.Id}&season={season}&episode={number}");
    }

    private async Task<string?> ResolveImdbIdAsync(EContentScheduleType contentType, int tmdbId)
    {
        var result = contentType == EContentScheduleType.MOVIE
            ? await tmdbService.GetMovieExternalIdsAsync(tmdbId)
            : await tmdbService.GetSeriesExternalIdsAsync(tmdbId);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Could not resolve IMDb id for {ContentType} TMDB {TmdbId}: {Error}",
                contentType, tmdbId, result.Error.Description);
            return null;
        }

        return result.Data;
    }

    private static Error? Validate(CreateContentScheduleRequestDto dto)
    {
        if (!Enum.IsDefined(dto.ContentType))
            return new Error("400", "Tipo de conteúdo inválido.");

        if (dto.TmdbId <= 0)
            return new Error("400", "Conteúdo do TMDB inválido.");

        if (string.IsNullOrWhiteSpace(dto.Title))
            return new Error("400", "Informe o título do conteúdo.");

        if (dto.ScheduledDate == default)
            return new Error("400", "Informe a data prevista.");

        if (dto.ContentType == EContentScheduleType.EPISODE)
        {
            if (dto.SeasonNumber is null || dto.EpisodeNumber is null || dto.EpisodeNumber <= 0)
                return new Error("400", "Informe a temporada e o número do episódio.");
        }

        return null;
    }
}
