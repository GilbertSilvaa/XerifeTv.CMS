namespace XerifeTv.CMS.Modules.ContentCalendar.Services;

public sealed class BusinessClock : IBusinessClock
{
    private const string TimeZoneId = "America/Sao_Paulo";

    private static readonly TimeZoneInfo OperationTimeZone = ResolveTimeZone();

    public TimeZoneInfo TimeZone => OperationTimeZone;

    public DateTime Now
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, OperationTimeZone);

    public DateOnly Today => DateOnly.FromDateTime(Now);

    private static TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone(
                TimeZoneId,
                TimeSpan.FromHours(-3),
                "America/Sao_Paulo",
                "America/Sao_Paulo");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.CreateCustomTimeZone(
                TimeZoneId,
                TimeSpan.FromHours(-3),
                "America/Sao_Paulo",
                "America/Sao_Paulo");
        }
    }
}
