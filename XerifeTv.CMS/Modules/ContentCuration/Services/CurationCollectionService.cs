using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Content.Services;
using XerifeTv.CMS.Modules.ContentCuration.Dtos.Request;
using XerifeTv.CMS.Modules.ContentCuration.Dtos.Response;
using XerifeTv.CMS.Modules.ContentCuration.Entities;
using XerifeTv.CMS.Modules.ContentCuration.Enums;
using XerifeTv.CMS.Modules.ContentCuration.Filters;
using XerifeTv.CMS.Modules.ContentCuration.Interfaces;
using XerifeTv.CMS.Modules.ContentCuration.Models;
using XerifeTv.CMS.Modules.ContentCuration.Services;

namespace XerifeTv.CMS.Modules.ContentCuration;

public sealed class CurationCollectionService(
    ICurationCollectionRepository repository,
    ICurationSettingsRepository settingsRepository,
    ICurationContentRepository contentRepository,
    ICurationContentResolver contentResolver,
    ICurationCollectionResolver collectionResolver,
    CategoryDistributionCalculator categoryDistributionCalculator) : ICurationCollectionService
{
    public async Task<Result<PagedList<GetCurationCollectionResponseDto>>> GetAsync(
        int currentPage, int limit)
    {
        try
        {
            currentPage = Math.Max(1, currentPage);
            limit = Math.Max(1, limit);

            var all = await repository.GetOrderedAsync(onlyActive: false);

            var pageItems = all
                .Skip(limit * (currentPage - 1))
                .Take(limit)
                .ToList();

            var responses = new List<GetCurationCollectionResponseDto>(pageItems.Count);

            foreach (var item in pageItems)
                responses.Add(await BuildResponseAsync(item));

            var totalPages = (int)Math.Ceiling(all.Count / (decimal)limit);

            return Result<PagedList<GetCurationCollectionResponseDto>>.Success(
                new PagedList<GetCurationCollectionResponseDto>(currentPage, totalPages, responses));
        }
        catch (Exception ex)
        {
            return Result<PagedList<GetCurationCollectionResponseDto>>.Failure(new("500", ex.Message));
        }
    }

    public async Task<Result<GetCurationCollectionResponseDto?>> GetAsync(string id)
    {
        try
        {
            var entity = await repository.GetAsync(id);

            if (entity is null)
                return Result<GetCurationCollectionResponseDto?>.Failure(
                    new("404", "Coleção não encontrada."));

            return Result<GetCurationCollectionResponseDto?>.Success(
                await BuildResponseAsync(entity));
        }
        catch (Exception ex)
        {
            return Result<GetCurationCollectionResponseDto?>.Failure(new("500", ex.Message));
        }
    }

    public async Task<Result<IReadOnlyList<GetCurationCollectionResponseDto>>> GetAllAsync(bool onlyActive)
    {
        try
        {
            var entities = await repository.GetOrderedAsync(onlyActive);
            var responses = new List<GetCurationCollectionResponseDto>(entities.Count);

            foreach (var entity in entities)
                responses.Add(await BuildResponseAsync(entity));

            return Result<IReadOnlyList<GetCurationCollectionResponseDto>>.Success(responses);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<GetCurationCollectionResponseDto>>.Failure(
                new("500", ex.Message));
        }
    }

    public async Task<Result<string>> CreateAsync(SaveCurationCollectionRequestDto dto)
    {
        try
        {
            var validation = await ValidateAsync(dto);

            if (validation is not null)
                return Result<string>.Failure(new("400", validation));

            var types = NormalizeContentTypes(dto.ContentTypes);

            var entity = new CurationCollectionEntity
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                ContentTypes = types,
                Mode = dto.Mode,
                Limit = dto.Limit > 0 ? dto.Limit : CurationConstants.DefaultCollectionLimit,
                AllowsPagination = dto.AllowsPagination,
                OrderBy = dto.OrderBy,
                SortDirection = dto.SortDirection,
                LogicalOperator = dto.LogicalOperator,
                Order = dto.Order > 0 ? dto.Order : await NextOrderAsync(),
                IsActive = dto.IsActive,
                Items = dto.Mode == ECurationCollectionMode.STATIC
                    ? NormalizeItems(dto.Items, types)
                    : [],
                Filters = dto.Mode == ECurationCollectionMode.DYNAMIC
                    ? BuildFilters(dto.Filters)
                    : []
            };

            var id = await repository.CreateAsync(entity);
            await settingsRepository.IncrementVersionAsync();

            return Result<string>.Success(id);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure(new("500", ex.Message));
        }
    }

    public async Task<Result<string>> UpdateAsync(SaveCurationCollectionRequestDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Id))
                return Result<string>.Failure(new("400", "Identificador da coleção é obrigatório."));

            var validation = await ValidateAsync(dto);

            if (validation is not null)
                return Result<string>.Failure(new("400", validation));

            var entity = await repository.GetAsync(dto.Id);

            if (entity is null)
                return Result<string>.Failure(new("404", "Coleção não encontrada."));

            var types = NormalizeContentTypes(dto.ContentTypes);

            entity.Name = dto.Name.Trim();
            entity.Description = dto.Description?.Trim();
            entity.ContentTypes = types;
            entity.Mode = dto.Mode;
            entity.Limit = dto.Limit > 0 ? dto.Limit : CurationConstants.DefaultCollectionLimit;
            entity.AllowsPagination = dto.AllowsPagination;
            entity.OrderBy = dto.OrderBy;
            entity.SortDirection = dto.SortDirection;
            entity.LogicalOperator = dto.LogicalOperator;
            entity.Order = dto.Order > 0 ? dto.Order : entity.Order;
            entity.IsActive = dto.IsActive;
            entity.Items = dto.Mode == ECurationCollectionMode.STATIC
                ? NormalizeItems(dto.Items, types)
                : [];
            entity.Filters = dto.Mode == ECurationCollectionMode.DYNAMIC
                ? BuildFilters(dto.Filters)
                : [];

            await repository.UpdateAsync(entity);
            await settingsRepository.IncrementVersionAsync();

            return Result<string>.Success(entity.Id);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure(new("500", ex.Message));
        }
    }

    public async Task<Result<bool>> DeleteAsync(string id)
    {
        try
        {
            var entity = await repository.GetAsync(id);

            if (entity is null)
                return Result<bool>.Failure(new("404", "Coleção não encontrada."));

            await repository.DeleteAsync(id);
            await settingsRepository.IncrementVersionAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(new("500", ex.Message));
        }
    }

    public async Task<Result<bool>> ToggleActiveAsync(string id)
    {
        try
        {
            var entity = await repository.GetAsync(id);

            if (entity is null)
                return Result<bool>.Failure(new("404", "Coleção não encontrada."));

            entity.IsActive = !entity.IsActive;

            await repository.UpdateAsync(entity);
            await settingsRepository.IncrementVersionAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(new("500", ex.Message));
        }
    }

    public async Task<Result<bool>> MoveAsync(string id, int direction)
    {
        try
        {
            var ordered = (await repository.GetOrderedAsync(onlyActive: false)).ToList();
            var index = ordered.FindIndex(x => x.Id == id);

            if (index < 0)
                return Result<bool>.Failure(new("404", "Coleção não encontrada."));

            var targetIndex = index + Math.Sign(direction);

            if (targetIndex < 0 || targetIndex >= ordered.Count)
                return Result<bool>.Success(true);

            var current = ordered[index];
            var target = ordered[targetIndex];

            (current.Order, target.Order) = (target.Order, current.Order);

            if (current.Order == target.Order)
            {
                current.Order = index + 1;
                target.Order = targetIndex + 1;
            }

            await repository.UpdateAsync(current);
            await repository.UpdateAsync(target);
            await settingsRepository.IncrementVersionAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(new("500", ex.Message));
        }
    }

    public async Task<Result<CurationCollectionPreviewResponseDto?>> PreviewAsync(string id)
    {
        try
        {
            var entity = await repository.GetAsync(id);

            if (entity is null)
                return Result<CurationCollectionPreviewResponseDto?>.Failure(
                    new("404", "Coleção não encontrada."));

            var resolved = await collectionResolver.ResolveItemsAsync(entity);

            return Result<CurationCollectionPreviewResponseDto?>.Success(
                new CurationCollectionPreviewResponseDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Limit = entity.Limit,
                    Items = [.. resolved.Select(ToPreviewItem)]
                });
        }
        catch (Exception ex)
        {
            return Result<CurationCollectionPreviewResponseDto?>.Failure(new("500", ex.Message));
        }
    }

    public async Task<Result<IReadOnlyList<GetCurationContentResponseDto>>> SearchContentsAsync(
        SearchCurationContentRequestDto dto)
    {
        try
        {
            var limit = dto.Limit > 0 ? dto.Limit : 20;

            if (dto.ContentType == EContentType.MOVIE)
            {
                var movies = await contentRepository.SearchMoviesAsync(dto.Term, limit);

                return Result<IReadOnlyList<GetCurationContentResponseDto>>.Success(
                    [.. movies.Select(m => new GetCurationContentResponseDto
                    {
                        Id = m.Id,
                        ContentType = EContentType.MOVIE,
                        Title = m.Title,
                        PosterUrl = m.PosterUrl,
                        BannerUrl = m.BannerUrl,
                        ReleaseYear = m.ReleaseYear,
                        Disabled = m.Disabled
                    })]);
            }

            var series = await contentRepository.SearchSeriesAsync(dto.Term, limit);

            return Result<IReadOnlyList<GetCurationContentResponseDto>>.Success(
                [.. series.Select(s => new GetCurationContentResponseDto
                {
                    Id = s.Id,
                    ContentType = EContentType.SERIES,
                    Title = s.Title,
                    PosterUrl = s.PosterUrl,
                    BannerUrl = s.BannerUrl,
                    ReleaseYear = s.ReleaseYear,
                    Disabled = s.Disabled
                })]);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<GetCurationContentResponseDto>>.Failure(
                new("500", ex.Message));
        }
    }

    public async Task<Result<CurationReorderResponseDto>> ReorderAutomaticallyAsync()
    {
        try
        {
            var all = (await repository.GetOrderedAsync(onlyActive: false)).ToList();

            var active = all
                .Where(x => x.IsActive)
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var inactive = all
                .Where(x => !x.IsActive)
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var inputs = new List<CategoryDistributionInput>(active.Count);
            var empty = new List<CurationCollectionEntity>();

            foreach (var collection in active)
            {
                var resolved = await collectionResolver.ResolveItemsAsync(collection);

                if (resolved.Count == 0)
                {
                    empty.Add(collection);
                    continue;
                }

                inputs.Add(new CategoryDistributionInput(
                    collection.Id,
                    [.. resolved.Select(r => $"{r.ContentType}:{r.ContentId}")]));
            }

            var distribution = categoryDistributionCalculator.Calculate(inputs);

            var finalOrder = new List<string>(active.Count + inactive.Count);
            finalOrder.AddRange(distribution.Ordered);
            finalOrder.AddRange(distribution.Redundant);
            finalOrder.AddRange(empty.Select(x => x.Id));
            finalOrder.AddRange(inactive.Select(x => x.Id));

            var byId = all.ToDictionary(x => x.Id);

            for (var index = 0; index < finalOrder.Count; index++)
            {
                var collection = byId[finalOrder[index]];
                var newOrder = index + 1;

                if (collection.Order == newOrder)
                    continue;

                collection.Order = newOrder;
                await repository.UpdateAsync(collection);
            }

            await settingsRepository.IncrementVersionAsync();

            return Result<CurationReorderResponseDto>.Success(
                new CurationReorderResponseDto(
                    active.Count,
                    distribution.Ordered.Count,
                    distribution.Redundant.Count,
                    empty.Count,
                    inactive.Count));
        }
        catch (Exception ex)
        {
            return Result<CurationReorderResponseDto>.Failure(new("500", ex.Message));
        }
    }

    public async Task<Result<string[]>> GetActiveCategoryNamesAsync()
    {
        try
        {
            var entities = await repository.GetOrderedAsync(onlyActive: true);
            return Result<string[]>.Success([.. entities.Select(x => x.Name)]);
        }
        catch (Exception ex)
        {
            return Result<string[]>.Failure(new("500", ex.Message));
        }
    }

    public async Task<Result<CurationCollectionEntity?>> GetActiveByNameAsync(string name)
    {
        try
        {
            var entity = await repository.GetByNameAsync(name);

            if (entity is null || !entity.IsActive)
                return Result<CurationCollectionEntity?>.Success(null);

            return Result<CurationCollectionEntity?>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<CurationCollectionEntity?>.Failure(new("500", ex.Message));
        }
    }

    private async Task<GetCurationCollectionResponseDto> BuildResponseAsync(
        CurationCollectionEntity entity)
    {
        var items = entity.Mode == ECurationCollectionMode.STATIC
            ? await MapSelectedItemsAsync(entity.Items)
            : [];

        return GetCurationCollectionResponseDto.FromEntity(entity, items);
    }

    private async Task<IReadOnlyList<GetCurationItemResponseDto>> MapSelectedItemsAsync(
        IEnumerable<CollectionItem> items)
    {
        var ordered = items.OrderBy(x => x.Order).ToList();

        var references = ordered
            .Select(x => new CurationContentReference(x.ContentId, x.ContentType))
            .ToList();

        var resolved = await contentResolver.ResolveAsync(references);

        return [.. resolved.Select((item, index) => new GetCurationItemResponseDto
        {
            ContentId = item.ContentId,
            ContentType = item.ContentType,
            Order = index + 1,
            Title = item.Title,
            PosterUrl = item.PosterUrl
        })];
    }

    private static CurationPreviewItemResponseDto ToPreviewItem(ResolvedCurationContent item)
    {
        return new()
        {
            ContentId = item.ContentId,
            ContentType = item.ContentType,
            Title = item.Title,
            PosterUrl = item.PosterUrl,
            BannerUrl = item.BannerUrl,
            Synopsis = item.Synopsis,
            ReleaseYear = item.ReleaseYear,
            CreateAt = item.CreateAt
        };
    }

    private async Task<int> NextOrderAsync()
    {
        var entities = await repository.GetOrderedAsync(onlyActive: false);
        return entities.Count == 0 ? 1 : entities.Max(x => x.Order) + 1;
    }

    private async Task<string?> ValidateAsync(SaveCurationCollectionRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return "Informe o nome da coleção.";

        if (await repository.NameExistsAsync(dto.Name, dto.Id))
            return "Já existe uma coleção com esse nome.";

        var types = NormalizeContentTypes(dto.ContentTypes);

        if (types.Count == 0)
            return "Selecione pelo menos um tipo de conteúdo elegível.";

        if (dto.ContentTypes.Any(t => !Enum.IsDefined(t)))
            return "Tipo de conteúdo inválido.";

        if (dto.Mode == ECurationCollectionMode.DYNAMIC)
        {
            var errors = CurationFilterValidator.Validate(BuildFilters(dto.Filters), types);

            if (errors.Count > 0)
                return string.Join(" ", errors);
        }

        return null;
    }

    private static List<EContentType> NormalizeContentTypes(IEnumerable<EContentType> contentTypes)
    {
        return [.. contentTypes
            .Where(x => Enum.IsDefined(x))
            .Distinct()];
    }

    private static List<CollectionItem> NormalizeItems(
        IEnumerable<CurationContentItemRequestDto> items, IReadOnlyList<EContentType> types)
    {
        return [.. items
            .Where(x => !string.IsNullOrWhiteSpace(x.ContentId)
                && Enum.IsDefined(x.ContentType)
                && types.Contains(x.ContentType))
            .OrderBy(x => x.Order)
            .GroupBy(x => (x.ContentId, x.ContentType))
            .Select(g => g.First())
            .Select((item, index) => new CollectionItem
            {
                ContentId = item.ContentId,
                ContentType = item.ContentType,
                Order = index + 1
            })];
    }

    private static List<DynamicFilterRule> BuildFilters(
        IEnumerable<SaveDynamicFilterRuleRequestDto> filters)
    {
        var rules = new List<DynamicFilterRule>();

        foreach (var filter in filters)
        {
            var property = CurationFilterCatalog.Find(filter.Property);

            rules.Add(new DynamicFilterRule
            {
                Property = property?.Name ?? filter.Property.Trim(),
                ValueType = property?.ValueType ?? ECurationFilterValueType.TEXT,
                Operator = filter.Operator,
                Value = filter.Value?.Trim(),
                SecondValue = filter.SecondValue?.Trim(),
                Order = rules.Count + 1
            });
        }

        return rules;
    }
}
