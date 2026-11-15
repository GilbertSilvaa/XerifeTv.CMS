using XerifeTv.CMS.Modules.ContentCuration.Entities;
using XerifeTv.CMS.Modules.ContentCuration.Enums;

namespace XerifeTv.CMS.Modules.ContentCuration.Dtos.Response;

public sealed class GetCurationCollectionResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<EContentType> ContentTypes { get; set; } = [];
    public ECurationCollectionMode Mode { get; set; }
    public int Limit { get; set; }
    public bool AllowsPagination { get; set; }
    public ECurationOrderBy OrderBy { get; set; }
    public ECurationSortDirection SortDirection { get; set; }
    public ECurationLogicalOperator LogicalOperator { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public List<GetCurationItemResponseDto> Items { get; set; } = [];
    public List<GetDynamicFilterRuleResponseDto> Filters { get; set; } = [];
    public DateTime CreateAt { get; set; }
    public DateTime? UpdateAt { get; set; }

    public static GetCurationCollectionResponseDto FromEntity(
        CurationCollectionEntity entity,
        IReadOnlyList<GetCurationItemResponseDto> items)
    {
        return new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            ContentTypes = [.. entity.ContentTypes],
            Mode = entity.Mode,
            Limit = entity.Limit,
            AllowsPagination = entity.AllowsPagination,
            OrderBy = entity.OrderBy,
            SortDirection = entity.SortDirection,
            LogicalOperator = entity.LogicalOperator,
            Order = entity.Order,
            IsActive = entity.IsActive,
            Items = [.. items],
            Filters = [.. entity.Filters
                .OrderBy(f => f.Order)
                .Select(GetDynamicFilterRuleResponseDto.FromEntity)],
            CreateAt = entity.CreateAt,
            UpdateAt = entity.UpdateAt
        };
    }
}

public sealed class GetDynamicFilterRuleResponseDto
{
    public string Property { get; set; } = string.Empty;
    public ECurationFilterValueType ValueType { get; set; }
    public ECurationFilterOperator Operator { get; set; }
    public string? Value { get; set; }
    public string? SecondValue { get; set; }

    public static GetDynamicFilterRuleResponseDto FromEntity(DynamicFilterRule entity)
    {
        return new()
        {
            Property = entity.Property,
            ValueType = entity.ValueType,
            Operator = entity.Operator,
            Value = entity.Value,
            SecondValue = entity.SecondValue
        };
    }
}
