using XerifeTv.CMS.Modules.Channel.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Dashboard.Dtos.Response;
using XerifeTv.CMS.Modules.Dashboard.Interfaces;
using XerifeTv.CMS.Modules.Movie.Interfaces;
using XerifeTv.CMS.Modules.Series.Interfaces;

namespace XerifeTv.CMS.Modules.Dashboard;

public sealed class DashboardService(
  IMovieRepository _movieRepository,
  ISeriesRepository _seriesRepository,
  IChannelRepository _channelRepository) : IDashboardService
{
    public async Task<Result<GetDashboardDataRequestDto>> Get()
    {
        var response = await Task.WhenAll([
          _movieRepository.CountAsync(),
          _seriesRepository.CountAsync(),
          _channelRepository.CountAsync()
        ]);

        return Result<GetDashboardDataRequestDto>.Success(
          new GetDashboardDataRequestDto(response[0], response[1], response[2]));
    }
}
