using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Globalization;
using XerifeTv.CMS.Modules.AuditLog.Interfaces;
using XerifeTv.CMS.Modules.Channel.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Dashboard.Dtos.Response;
using XerifeTv.CMS.Modules.Dashboard.Interfaces;
using XerifeTv.CMS.Modules.Movie.Enums;
using XerifeTv.CMS.Modules.Movie.Interfaces;
using XerifeTv.CMS.Modules.Series.Enums;
using XerifeTv.CMS.Modules.Series.Interfaces;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.Dashboard;

public sealed class DashboardService(
  IMovieRepository movieRepository,
  ISeriesRepository seriesRepository,
  IChannelRepository channelRepository,
  IAuditLogService auditLogService,
  IMongoClient mongoClient,
  IOptions<DBSettings> dbSettings) : IDashboardService
{
    private const int LatestContentLimit = 6;

    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public async Task<Result<GetDashboardDataRequestDto>> GetAsync(string? userName = null)
    {
        var today = DateTime.UtcNow;
        var currentMonthStart = new DateTime(today.Year, today.Month, 1);

        var rangesLast6Months = BuildLast6MonthRanges(today, currentMonthStart);

        var totalMoviesTask = movieRepository.CountAsync();
        var totalSeriesTask = seriesRepository.CountAsync();
        var totalChannelsTask = channelRepository.CountAsync();

        var monthlyCountTasks = rangesLast6Months
            .Select(range => Task.WhenAll(
                movieRepository.CountByDateRangeAsync(range.Start, range.End),
                seriesRepository.CountByDateRangeAsync(range.Start, range.End),
                channelRepository.CountByDateRangeAsync(range.Start, range.End)))
            .ToArray();

        var lastMoviesTask = movieRepository.GetByFilterAsync(new(
            EMovieSearchFilter.TITLE,
            EMovieOrderFilter.REGISTRATION_DATE_DESC,
            search: "",
            limitResults: LatestContentLimit,
            currentPage: 1,
            isIncludeDisabled: true));

        var lastSeriesTask = seriesRepository.GetByFilterAsync(new(
            ESeriesSearchFilter.TITLE,
            search: "",
            limitResults: LatestContentLimit,
            currentPage: 1,
            isIncludeDisabled: true));

        var lastChannelsTask = channelRepository.GetAsync(currentPage: 1, LatestContentLimit);

        var auditLogsTask = userName is not null
            ? auditLogService.GetAsync(userName, currentPage: 1, LatestContentLimit)
            : auditLogService.GetAsync(currentPage: 1, LatestContentLimit);

        var dbSizeTask = GetDatabaseSizeInMbAsync();

        List<Task> pendingTasks =
        [
            totalMoviesTask, totalSeriesTask, totalChannelsTask,
            lastMoviesTask, lastSeriesTask, lastChannelsTask,
            auditLogsTask, dbSizeTask,
            .. monthlyCountTasks
        ];

        await Task.WhenAll(pendingTasks);

        var monthlyCounts = monthlyCountTasks.Select(t => t.Result).ToArray();
        var currentMonthCounts = monthlyCounts[^1];

        var countsTotalContentByMonths = rangesLast6Months.Select((range, index) => new
        {
            range.MonthName,
            Count = monthlyCounts[index].Sum()
        });

        List<LatestContentDto> lastContentsAdded = [];

        var lastMoviesAdded = lastMoviesTask.Result;
        var lastSeriesAdded = lastSeriesTask.Result;
        var lastChannelsAdded = lastChannelsTask.Result;

        if (lastMoviesAdded?.Items?.Any() == true)
        {
            lastContentsAdded.AddRange(lastMoviesAdded.Items.Select(m => new LatestContentDto(m.Title, m.CreateAt, ELatestContentType.MOVIE)));
        }

        if (lastSeriesAdded?.Items?.Any() == true)
        {
            lastContentsAdded.AddRange(lastSeriesAdded.Items.Select(s => new LatestContentDto(s.Title, s.CreateAt, ELatestContentType.SERIES)));
        }

        if (lastChannelsAdded?.Items?.Any() == true)
        {
            lastContentsAdded.AddRange(lastChannelsAdded.Items.Select(c => new LatestContentDto(c.Title, c.CreateAt, ELatestContentType.CHANNEL)));
        }

        var auditLogsResponse = auditLogsTask.Result;

        List<LatestSystemActionDto> lastSystemActions = auditLogsResponse.IsSuccess && auditLogsResponse.Data?.Items is not null
            ? [.. auditLogsResponse.Data.Items.Select(x => new LatestSystemActionDto(x.UserName, x.Description, x.CreateAt))]
            : [];

        var result = new GetDashboardDataRequestDto(
            LatestSystemActions: lastSystemActions,
            MonthlyContentCounts: [.. countsTotalContentByMonths.Select(x => new MonthlyContentCountDto(x.MonthName, x.Count))],
            LatestContents: [.. lastContentsAdded.OrderByDescending(c => c.PublishAt).Take(LatestContentLimit)],
            NumberOfMoviesTotal: totalMoviesTask.Result,
            NumberOfSeriesTotal: totalSeriesTask.Result,
            NumberOfChannelsTotal: totalChannelsTask.Result,
            NumberOfMoviesAddedCurrentMonth: currentMonthCounts[0],
            NumberOfSeriesAddedCurrentMonth: currentMonthCounts[1],
            NumberOfChannelsAddedCurrentMonth: currentMonthCounts[2],
            DataBaseSizeInMb: dbSizeTask.Result
        );

        return Result<GetDashboardDataRequestDto>.Success(result);
    }

    private static List<MonthRange> BuildLast6MonthRanges(DateTime today, DateTime currentMonthStart)
    {
        return Enumerable.Range(0, 6)
            .Select(i =>
            {
                var start = currentMonthStart.AddMonths(-5 + i);
                var end = i == 5
                    ? today
                    : start.AddMonths(1).AddDays(-1);

                return new MonthRange(
                    PtBr.TextInfo.ToTitleCase(start.ToString("MMMM", PtBr)),
                    start,
                    end);
            })
            .ToList();
    }

    private async Task<double> GetDatabaseSizeInMbAsync()
    {
        var database = mongoClient.GetDatabase(dbSettings.Value.DatabaseName);

        var stats = await database.RunCommandAsync<BsonDocument>(
            new BsonDocument("dbStats", 1));

        var storageSizeBytes = stats["storageSize"].ToInt64();

        return storageSizeBytes / 1024d / 1024d;
    }

    private sealed record MonthRange(string MonthName, DateTime Start, DateTime End);
}