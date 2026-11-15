using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.ContentCuration.Dtos.Request;
using XerifeTv.CMS.Modules.ContentCuration.Dtos.Response;
using XerifeTv.CMS.Modules.ContentCuration.Entities;
using XerifeTv.CMS.Modules.ContentCuration.Enums;
using XerifeTv.CMS.Modules.ContentCuration.Interfaces;
using XerifeTv.CMS.Modules.ContentCuration.Models;
using XerifeTv.CMS.Modules.ContentCuration.Services;

namespace XerifeTv.CMS.Modules.ContentCuration;

public sealed class FeaturedService(
    IFeaturedRepository repository,
    ICurationSettingsRepository settingsRepository,
    ICurationContentResolver contentResolver) : IFeaturedService
{
    public async Task<Result<PagedList<GetFeaturedResponseDto>>> GetAsync(int currentPage, int limit)
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

            var responses = new List<GetFeaturedResponseDto>(pageItems.Count);

            foreach (var item in pageItems)
                responses.Add(await BuildResponseAsync(item));

            var totalPages = (int)Math.Ceiling(all.Count / (decimal)limit);

            return Result<PagedList<GetFeaturedResponseDto>>.Success(
                new PagedList<GetFeaturedResponseDto>(currentPage, totalPages, responses));
        }
        catch (Exception ex)
        {
            return Result<PagedList<GetFeaturedResponseDto>>.Failure(new("500", ex.Message));
        }
    }

    public async Task<Result<GetFeaturedResponseDto?>> GetAsync(string id)
    {
        try
        {
            var entity = await repository.GetAsync(id);

            if (entity is null)
                return Result<GetFeaturedResponseDto?>.Failure(new("404", "Destaque não encontrado."));

            return Result<GetFeaturedResponseDto?>.Success(await BuildResponseAsync(entity));
        }
        catch (Exception ex)
        {
            return Result<GetFeaturedResponseDto?>.Failure(new("500", ex.Message));
        }
    }

    public async Task<Result<IReadOnlyList<GetFeaturedResponseDto>>> GetAllAsync(bool onlyActive)
    {
        try
        {
            var entities = await repository.GetOrderedAsync(onlyActive);
            var responses = new List<GetFeaturedResponseDto>(entities.Count);

            foreach (var entity in entities)
                responses.Add(await BuildResponseAsync(entity));

            return Result<IReadOnlyList<GetFeaturedResponseDto>>.Success(responses);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<GetFeaturedResponseDto>>.Failure(new("500", ex.Message));
        }
    }

    public async Task<Result<string>> CreateAsync(SaveFeaturedRequestDto dto)
    {
        try
        {
            var validation = Validate(dto);
            if (validation is not null)
                return Result<string>.Failure(new("400", validation));

            var maxItems = dto.MaxItems > 0
                ? dto.MaxItems
                : CurationConstants.DefaultFeaturedMaxItems;

            if (dto.IsHomeHero && dto.IsActive)
                await repository.UnsetHomeHeroAsync();

            var entity = new FeaturedEntity
            {
                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                IsHomeHero = dto.IsHomeHero && dto.IsActive,
                Order = dto.Order > 0 ? dto.Order : await NextOrderAsync(),
                IsActive = dto.IsActive,
                MaxItems = maxItems,
                Items = NormalizeItems(dto.Items, maxItems)
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

    public async Task<Result<string>> UpdateAsync(SaveFeaturedRequestDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Id))
                return Result<string>.Failure(new("400", "Identificador do destaque é obrigatório."));

            var validation = Validate(dto);
            if (validation is not null)
                return Result<string>.Failure(new("400", validation));

            var entity = await repository.GetAsync(dto.Id);

            if (entity is null)
                return Result<string>.Failure(new("404", "Destaque não encontrado."));

            var maxItems = dto.MaxItems > 0
                ? dto.MaxItems
                : CurationConstants.DefaultFeaturedMaxItems;

            if (dto.IsHomeHero && dto.IsActive)
                await repository.UnsetHomeHeroAsync(entity.Id);

            entity.Title = dto.Title.Trim();
            entity.Description = dto.Description.Trim();
            entity.IsHomeHero = dto.IsHomeHero && dto.IsActive;
            entity.Order = dto.Order > 0 ? dto.Order : entity.Order;
            entity.IsActive = dto.IsActive;
            entity.MaxItems = maxItems;
            entity.Items = NormalizeItems(dto.Items, maxItems);

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
                return Result<bool>.Failure(new("404", "Destaque não encontrado."));

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
                return Result<bool>.Failure(new("404", "Destaque não encontrado."));

            entity.IsActive = !entity.IsActive;

            if (!entity.IsActive)
                entity.IsHomeHero = false;

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
                return Result<bool>.Failure(new("404", "Destaque não encontrado."));

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

    public async Task<Result<GetFeaturedResponseDto?>> GetHomeHeroAsync()
    {
        try
        {
            var entity = await repository.GetHomeHeroAsync();

            if (entity is null)
            {
                var firstActive = (await repository.GetOrderedAsync(onlyActive: true)).FirstOrDefault();
                return Result<GetFeaturedResponseDto?>.Success(
                    firstActive is null ? null : await BuildResponseAsync(firstActive));
            }

            return Result<GetFeaturedResponseDto?>.Success(await BuildResponseAsync(entity));
        }
        catch (Exception ex)
        {
            return Result<GetFeaturedResponseDto?>.Failure(new("500", ex.Message));
        }
    }

    private async Task<GetFeaturedResponseDto> BuildResponseAsync(FeaturedEntity entity)
    {
        var items = entity.Items
            .OrderBy(x => x.Order)
            .ToList();

        var references = items
            .Select(x => new CurationContentReference(x.ContentId, x.ContentType))
            .ToList();

        var resolved = await contentResolver.ResolveAsync(references);

        var itemResponses = resolved
            .Select((item, index) => new GetCurationItemResponseDto
            {
                ContentId = item.ContentId,
                ContentType = item.ContentType,
                Order = index + 1,
                Title = item.Title,
                PosterUrl = item.PosterUrl
            })
            .ToList();

        return GetFeaturedResponseDto.FromEntity(entity, itemResponses);
    }

    private async Task<int> NextOrderAsync()
    {
        var entities = await repository.GetOrderedAsync(onlyActive: false);
        return entities.Count == 0 ? 1 : entities.Max(x => x.Order) + 1;
    }

    private static List<FeaturedItem> NormalizeItems(
        IEnumerable<CurationContentItemRequestDto> items, int maxItems)
    {
        return items
            .Where(x => !string.IsNullOrWhiteSpace(x.ContentId) && Enum.IsDefined(x.ContentType))
            .OrderBy(x => x.Order)
            .GroupBy(x => (x.ContentId, x.ContentType))
            .Select(g => g.First())
            .Take(Math.Max(1, maxItems))
            .Select((item, index) => new FeaturedItem
            {
                ContentId = item.ContentId,
                ContentType = item.ContentType,
                Order = index + 1
            })
            .ToList();
    }

    private static string? Validate(SaveFeaturedRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return "Informe o título do destaque.";

        if (dto.MaxItems < 0)
            return "A quantidade máxima de conteúdos não pode ser negativa.";

        return null;
    }
}
