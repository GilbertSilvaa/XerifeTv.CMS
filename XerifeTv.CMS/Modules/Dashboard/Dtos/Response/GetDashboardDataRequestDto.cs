namespace XerifeTv.CMS.Modules.Dashboard.Dtos.Response;

public record class GetDashboardDataRequestDto(
  long NumberOfMovies,
  long NumberOfSeries,
  long NumberOfChannels);