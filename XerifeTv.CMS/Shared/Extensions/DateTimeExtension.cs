namespace XerifeTv.CMS.Shared.Extensions;

public static class DateTimeExtension
{
    public static string ToRelativeString(this DateTime dateTime)
    {
        var ts = DateTime.UtcNow - dateTime;

        if (ts.TotalSeconds < 10)
            return "agora mesmo";
        if (ts.TotalSeconds < 60)
            return $"há {Math.Floor(ts.TotalSeconds)}s";
        if (ts.TotalMinutes < 60)
            return $"há {Math.Floor(ts.TotalMinutes)}min";
        if (ts.TotalHours < 24)
            return $"há {Math.Floor(ts.TotalHours)}h";
        if (ts.TotalDays < 30)
            return $"há {Math.Floor(ts.TotalDays)} dia{(ts.TotalDays < 2 ? "" : "s")}";

        return dateTime.ToLocalTime().ToString("dd/MM/yyyy");
    }
}
