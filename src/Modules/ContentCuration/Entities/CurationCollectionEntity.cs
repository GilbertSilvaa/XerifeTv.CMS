using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.ContentCuration.Enums;

namespace XerifeTv.CMS.Modules.ContentCuration.Entities;

public sealed class CurationCollectionEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<EContentType> ContentTypes { get; set; } = [];
    public ECurationCollectionMode Mode { get; set; } = ECurationCollectionMode.STATIC;
    public int Limit { get; set; } = CurationConstants.DefaultCollectionLimit;
    public bool AllowsPagination { get; set; } = true;
    public ECurationOrderBy OrderBy { get; set; } = ECurationOrderBy.CREATE_AT;
    public ECurationSortDirection SortDirection { get; set; } = ECurationSortDirection.DESC;
    public ECurationLogicalOperator LogicalOperator { get; set; } = ECurationLogicalOperator.AND;
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public List<CollectionItem> Items { get; set; } = [];
    public List<DynamicFilterRule> Filters { get; set; } = [];
}

public sealed class CollectionItem : BaseEntity
{
    public string ContentId { get; set; } = string.Empty;
    public EContentType ContentType { get; set; } = EContentType.MOVIE;
    public int Order { get; set; }
}

public sealed class DynamicFilterRule : BaseEntity
{
    public string Property { get; set; } = string.Empty;
    public ECurationFilterValueType ValueType { get; set; } = ECurationFilterValueType.TEXT;
    public ECurationFilterOperator Operator { get; set; } = ECurationFilterOperator.EQUALS;
    public string? Value { get; set; }
    public string? SecondValue { get; set; }
    public int Order { get; set; }
}
