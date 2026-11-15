using XerifeTv.CMS.Modules.ContentCuration.Entities;
using XerifeTv.CMS.Modules.ContentCuration.Enums;

namespace XerifeTv.CMS.Modules.ContentCuration.Dtos.Response;

public sealed class GetCurationItemResponseDto
{
    public string ContentId { get; set; } = string.Empty;
    public EContentType ContentType { get; set; } = EContentType.MOVIE;
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
}

public sealed class GetFeaturedResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsHomeHero { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public int MaxItems { get; set; }
    public List<GetCurationItemResponseDto> Items { get; set; } = [];
    public DateTime CreateAt { get; set; }
    public DateTime? UpdateAt { get; set; }

    public static GetFeaturedResponseDto FromEntity(
        FeaturedEntity entity,
        IReadOnlyList<GetCurationItemResponseDto> items)
    {
        return new()
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            IsHomeHero = entity.IsHomeHero,
            Order = entity.Order,
            IsActive = entity.IsActive,
            MaxItems = entity.MaxItems,
            Items = [.. items],
            CreateAt = entity.CreateAt,
            UpdateAt = entity.UpdateAt
        };
    }
}
