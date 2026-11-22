using XerifeTv.CMS.Modules.ContentCalendar.Enums;

namespace XerifeTv.CMS.Modules.ContentCalendar;

public static class ContentScheduleDisplay
{
    public static string GetTypeLabel(EContentScheduleType type) => type switch
    {
        EContentScheduleType.MOVIE => "Filme",
        EContentScheduleType.SERIES => "Série",
        EContentScheduleType.EPISODE => "Episódio",
        _ => type.ToString()
    };

    public static string GetTypeIcon(EContentScheduleType type) => type switch
    {
        EContentScheduleType.MOVIE => "fa-solid fa-film",
        EContentScheduleType.SERIES => "fa-solid fa-clapperboard",
        EContentScheduleType.EPISODE => "fa-solid fa-tv",
        _ => "fa-solid fa-circle"
    };

    public static string GetStatusLabel(EScheduleStatus status) => status switch
    {
        EScheduleStatus.SCHEDULED => "Agendado",
        EScheduleStatus.PENDING => "Pendente",
        EScheduleStatus.REGISTERED => "Cadastrado",
        _ => status.ToString()
    };

    public static string GetStatusCssClass(EScheduleStatus status) => status switch
    {
        EScheduleStatus.SCHEDULED => "text-bg-primary",
        EScheduleStatus.PENDING => "text-bg-warning",
        EScheduleStatus.REGISTERED => "text-bg-success",
        _ => "text-bg-secondary"
    };

    public static string GetOriginLabel(EScheduleOrigin origin) => origin switch
    {
        EScheduleOrigin.MANUAL => "Manual",
        EScheduleOrigin.AUTOMATIC_IMPORT => "Importação automática",
        _ => origin.ToString()
    };

    public static string GetEpisodeLabel(int? season, int? episode)
        => season.HasValue && episode.HasValue ? $"T{season:00}E{episode:00}" : string.Empty;
}
