using System.Globalization;
using XerifeTv.CMS.Modules.Channel.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Dashboard.Dtos.Response;
using XerifeTv.CMS.Modules.Dashboard.Interfaces;
using XerifeTv.CMS.Modules.Movie.Enums;
using XerifeTv.CMS.Modules.Movie.Interfaces;
using XerifeTv.CMS.Modules.Series.Enums;
using XerifeTv.CMS.Modules.Series.Interfaces;

namespace XerifeTv.CMS.Modules.Dashboard;

public sealed class DashboardService(
  IMovieRepository movieRepository,
  ISeriesRepository seriesRepository,
  IChannelRepository channelRepository) : IDashboardService
{
    public async Task<Result<GetDashboardDataRequestDto>> GetAsync()
    {
        var today = DateTime.Today;
        var startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var endDate = today;

        var responseCounts = await Task.WhenAll([
            movieRepository.CountAsync(),
            seriesRepository.CountAsync(),
            channelRepository.CountAsync(),
            movieRepository.CountByDateRangeAsync(startDate, endDate),
            seriesRepository.CountByDateRangeAsync(startDate, endDate),
            channelRepository.CountByDateRangeAsync(startDate, endDate),
        ]);

        var culture = new CultureInfo("pt-BR");
        var currentMonthStart = new DateTime(today.Year, today.Month, 1);

        var rangesLast6Months = Enumerable.Range(0, 6)
            .Select(i =>
            {
                var start = currentMonthStart.AddMonths(-5 + i);
                var end = i == 5
                    ? today
                    : start.AddMonths(1).AddDays(-1);

                return new
                {
                    MonthName = culture.TextInfo.ToTitleCase(start.ToString("MMMM", culture)),
                    Start = start,
                    End = end
                };
            })
            .ToList();

        var countsByMothsResponses = await Task.WhenAll(rangesLast6Months.Select(range =>
            Task.WhenAll(
                movieRepository.CountByDateRangeAsync(range.Start, range.End),
                seriesRepository.CountByDateRangeAsync(range.Start, range.End),
                channelRepository.CountByDateRangeAsync(range.Start, range.End)
            )
        ));

        var countsTotalContentByMonths = countsByMothsResponses.Select((x, index) =>
        {
            return new
            {
                MothName = rangesLast6Months[index].MonthName,
                Count = x.Aggregate(0, (acc, x) => acc + x)
            };
        });

        const int latestContentLimit = 6;

        var lastMoviesAdded = await movieRepository.GetByFilterAsync(new(
            EMovieSearchFilter.TITLE,
            EMovieOrderFilter.REGISTRATION_DATE_DESC,
            search: "",
            limitResults: latestContentLimit,
            currentPage: 1,
            isIncludeDisabled: true));

        var lastSeriesAdded = await seriesRepository.GetByFilterAsync(new(
            ESeriesSearchFilter.TITLE,
            search: "",
            limitResults: latestContentLimit,
            currentPage: 1,
            isIncludeDisabled: true));

        var lastChannelsAdded = await channelRepository.GetAsync(currentPage: 1, latestContentLimit);

        List<LatestContentDto> lastContentsAdded = [];

        if (lastMoviesAdded?.Items?.Count() > 0)
        {
            lastContentsAdded.AddRange(lastMoviesAdded.Items.Select(m => new LatestContentDto(m.Title, m.CreateAt, ELatestContentType.MOVIE)));
        }

        if (lastSeriesAdded?.Items?.Count() > 0)
        {
            lastContentsAdded.AddRange(lastSeriesAdded.Items.Select(s => new LatestContentDto(s.Title, s.CreateAt, ELatestContentType.SERIES)));
        }

        if (lastChannelsAdded?.Items?.Count() > 0)
        {
            lastContentsAdded.AddRange(lastChannelsAdded.Items.Select(c => new LatestContentDto(c.Title, c.CreateAt, ELatestContentType.CHANNEL)));
        }

        var result = new GetDashboardDataRequestDto(
            MonthlyContentCounts: [.. countsTotalContentByMonths.Select(x => new MonthlyContentCountDto(x.MothName, x.Count))],
            LatestContents: [.. lastContentsAdded.OrderByDescending(c => c.PublishAt).Take(latestContentLimit)],
            NumberOfMoviesTotal: responseCounts[0],
            NumberOfSeriesTotal: responseCounts[1],
            NumberOfChannelsTotal: responseCounts[2],
            NumberOfMoviesAddedCurrentMonth: responseCounts[3],
            NumberOfSeriesAddedCurrentMonth: responseCounts[4],
            NumberOfChannelsAddedCurrentMonth: responseCounts[5]
        );

        return Result<GetDashboardDataRequestDto>.Success(result);
    }
}

