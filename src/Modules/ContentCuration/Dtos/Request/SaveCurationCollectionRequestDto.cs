using XerifeTv.CMS.Modules.ContentCuration.Enums;

namespace XerifeTv.CMS.Modules.ContentCuration.Dtos.Request;

public sealed class SaveCurationCollectionRequestDto
{
    public string? Id { get; set; }
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
    public List<CurationContentItemRequestDto> Items { get; set; } = [];
    public List<SaveDynamicFilterRuleRequestDto> Filters { get; set; } = [];
}

public sealed class SaveDynamicFilterRuleRequestDto
{
    public string Property { get; set; } = string.Empty;
    public ECurationFilterOperator Operator { get; set; } = ECurationFilterOperator.EQUALS;
    public string? Value { get; set; }
    public string? SecondValue { get; set; }
}
