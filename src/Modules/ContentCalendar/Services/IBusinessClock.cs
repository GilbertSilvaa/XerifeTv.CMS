namespace XerifeTv.CMS.Modules.ContentCalendar.Services;

public interface IBusinessClock
{
    /// <summary>Current date in the CMS operation time zone (America/Sao_Paulo).</summary>
    DateOnly Today { get; }

    /// <summary>Current instant converted to the CMS operation time zone.</summary>
    DateTime Now { get; }

    TimeZoneInfo TimeZone { get; }
}
