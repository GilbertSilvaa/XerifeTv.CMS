using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.ContentCalendar.Dtos.Request;
using XerifeTv.CMS.Modules.ContentCalendar.Dtos.Response;

namespace XerifeTv.CMS.Modules.ContentCalendar.Interfaces;

public interface IContentScheduleService
{
    Task<Result<GetContentScheduleResponseDto>> CreateAsync(CreateContentScheduleRequestDto dto);
    Task<Result<IReadOnlyCollection<GetContentScheduleResponseDto>>> GetByPeriodAsync(DateOnly start, DateOnly end);
    Task<Result<ContentScheduleDetailsResponseDto>> GetDetailsAsync(string id);
    Task<Result<GetContentScheduleResponseDto>> UpdateDateAsync(UpdateContentScheduleDateRequestDto dto);
    Task<Result<bool>> DeleteAsync(string id);
    Task<Result<ContentScheduleShortcutDto>> GetShortcutAsync(string id);
    Task<Result<ContentScheduleDetailsResponseDto>> LinkAsync(LinkContentScheduleRequestDto dto);
    Task<Result<GetContentScheduleResponseDto?>> UpsertAutomaticEpisodeAsync(AutomaticEpisodeScheduleRequestDto dto);
    Task<Result<IReadOnlyCollection<GetContentScheduleResponseDto>>> GetPendingAsync(int limit);
}
