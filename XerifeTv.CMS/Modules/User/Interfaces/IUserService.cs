using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.User.Dtos.Request;
using XerifeTv.CMS.Modules.User.Dtos.Response;

namespace XerifeTv.CMS.Modules.User.Interfaces;

public interface IUserService
{
  Task<Result<string>> Register(RegisterUserRequestDto dto);
  Task<Result<LoginUserResponseDto>> Login(LoginUserRequestDto dto);
  Task<Result<PagedList<GetUserRequestDto>>> Get(int currentPage, int limit);
  Task<Result<bool>> Delete(string id);
  Task<Result<(string? newToken, string? newRefreshToken)>> TryRefreshSession(string refreshToken);
}
