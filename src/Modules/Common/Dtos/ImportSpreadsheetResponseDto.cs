namespace XerifeTv.CMS.Modules.Common.Dtos;

public record ImportSpreadsheetResponseDto(
    string[] ErrorList,
    int? TotalItemsCount = 0,
    int? SuccessCount = 0,
    int? FailCount = 0,
    int? ProcessedCount = 0,
    int ProgressCount = 0,
    bool IsCancelled = false);