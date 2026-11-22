namespace XerifeTv.CMS.Modules.ContentCuration.Dtos.Response;

public sealed record CurationReorderResponseDto(
    int ActiveCount,
    int OrderedCount,
    int RedundantCount,
    int EmptyCount,
    int InactiveCount);
