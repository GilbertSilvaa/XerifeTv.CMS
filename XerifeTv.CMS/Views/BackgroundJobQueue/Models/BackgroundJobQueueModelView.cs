using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Response;
using XerifeTv.CMS.Modules.User.Dtos.Response;

namespace XerifeTv.CMS.Views.BackgroundJobQueue.Models;

public class BackgroundJobQueueModelView
{
	public IEnumerable<GetBackgroundJobResponseDto> Jobs { get; set; } = [];
	public IEnumerable<GetUserResponseDto> Users { get; set; } = [];
}