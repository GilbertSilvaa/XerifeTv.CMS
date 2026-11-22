using XerifeTv.CMS.Modules.Authentication.Dtos.Request;
using XerifeTv.CMS.Modules.Authentication.Dtos.Response;
using XerifeTv.CMS.Modules.Common;

namespace XerifeTv.CMS.Modules.Authentication.Interfaces;

public interface IAuthService
{
    Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto dto);
    Task<Result<(string? newToken, string? newRefreshToken)>> TryRefreshSessionAsync(string refreshToken);
}
