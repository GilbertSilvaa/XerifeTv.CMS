using XerifeTv.CMS.Modules.Authentication.Dtos.Request;
using XerifeTv.CMS.Modules.Authentication.Dtos.Response;
using XerifeTv.CMS.Modules.Authentication.Enums;
using XerifeTv.CMS.Modules.Common;

namespace XerifeTv.CMS.Modules.Authentication.Interfaces;

public interface ILoginStrategy
{
	Task<Result<LoginResponseDto>> AuthenticateAsync(LoginRequestDto dto);
	bool CanHandle(ELoginProvider loginProvider);
}