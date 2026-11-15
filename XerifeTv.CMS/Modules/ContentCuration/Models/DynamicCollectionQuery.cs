using XerifeTv.CMS.Modules.ContentCuration.Entities;
using XerifeTv.CMS.Modules.ContentCuration.Enums;

namespace XerifeTv.CMS.Modules.ContentCuration.Models;

public sealed record DynamicCollectionQuery(
    IReadOnlyList<DynamicFilterRule> Filters,
    ECurationLogicalOperator LogicalOperator,
    ECurationOrderBy OrderBy,
    ECurationSortDirection SortDirection,
    int Limit);
