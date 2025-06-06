namespace XerifeTv.CMS.Modules.Common.Dtos;

public record ImportSpreadsheetResponseDto(
    int? SuccessCount,
    int? FailCount,
    string[] ErrorList,
    int ProgressCount);