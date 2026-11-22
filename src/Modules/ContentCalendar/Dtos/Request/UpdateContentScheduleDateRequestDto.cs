namespace XerifeTv.CMS.Modules.ContentCalendar.Dtos.Request;

public class UpdateContentScheduleDateRequestDto
{
    public string Id { get; init; } = string.Empty;
    public DateOnly ScheduledDate { get; init; }
}
