namespace XerifeTv.CMS.Modules.Content.Dtos.Response;

public record GetContentsByNameResponseDto(
    IEnumerable<GetMovieContentResponseDto> Movies,
    IEnumerable<GetSeriesContentResponseDto> Series,
    IEnumerable<GetChannelContentResponseDto> Channels);
