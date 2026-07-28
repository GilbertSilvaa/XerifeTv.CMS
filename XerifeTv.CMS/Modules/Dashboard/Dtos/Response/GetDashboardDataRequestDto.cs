namespace XerifeTv.CMS.Modules.Dashboard.Dtos.Response;

public record class GetDashboardDataRequestDto(
    List<MonthlyContentCountDto> MonthlyContentCounts,
    List<LatestContentDto> LatestContents,
    int NumberOfMoviesTotal = 0,
    int NumberOfSeriesTotal = 0,
    int NumberOfChannelsTotal = 0,
    int NumberOfMoviesAddedCurrentMonth = 0,
    int NumberOfSeriesAddedCurrentMonth = 0,
    int NumberOfChannelsAddedCurrentMonth = 0);

public record MonthlyContentCountDto(string MonthName, int Count);

public record LatestContentDto(string Title, DateTime PublishAt, ELatestContentType Type);

public enum ELatestContentType
{
    MOVIE,
    SERIES,
    CHANNEL
}