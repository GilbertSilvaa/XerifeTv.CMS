using XerifeTv.CMS.Modules.ContentCalendar.Enums;

namespace XerifeTv.CMS.Modules.ContentCalendar.Dtos.Response;

public class ContentScheduleDetailsResponseDto
{
    public GetContentScheduleResponseDto Schedule { get; init; } = default!;
    public ContentScheduleDiagnosticDto Diagnostic { get; init; } = default!;
}

public class ContentScheduleDiagnosticDto
{
    public EContentScheduleDiagnosticStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? InternalContentId { get; init; }
    public bool CanLinkManually { get; init; }
}
