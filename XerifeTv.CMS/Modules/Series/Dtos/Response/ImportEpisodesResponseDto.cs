namespace XerifeTv.CMS.Modules.Series.Dtos.Response;

public record ImportEpisodesResponseDto(
	int TotalItemsCount,
	int ImportedCount,
	int ProgressCount,
	int ProcessedCount);