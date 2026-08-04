namespace XerifeTv.CMS.Modules.Series.Dtos.Request;

public record CancelImportEpisodesRequestDto(string ImportId, string SeriesId, string SeriesTitle);
