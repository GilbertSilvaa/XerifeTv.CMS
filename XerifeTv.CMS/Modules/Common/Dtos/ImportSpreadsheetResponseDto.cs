namespace XerifeTv.CMS.Modules.Common.Dtos;

public record ImportSpreadsheetResponseDto(
    int? TotalItemsCount,
    int? SuccessCount,
    int? FailCount,
	int? ProcessedCount,
	string[] ErrorList,
    int ProgressCount);