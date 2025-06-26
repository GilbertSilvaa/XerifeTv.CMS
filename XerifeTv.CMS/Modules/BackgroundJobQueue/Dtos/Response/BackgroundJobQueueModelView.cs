using XerifeTv.CMS.Modules.User.Dtos.Response;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Response;

public class BackgroundJobQueueModelView
{
	public IEnumerable<GetBackgroundJobResponseDto> Jobs { get; set; } = [];
	public IEnumerable<GetUserResponseDto> Users { get; set; } = [];
}