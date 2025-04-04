namespace XerifeTv.CMS.Shared.Helpers;

public class DateTimeHelper
{
  public static string ConvertSecondsToHHmm(long seconds)
  {
    var hours = seconds / 3600;
    var minutes = seconds % 3600 / 60;

    return $"{hours}h {minutes:D2}min";
  }
}
