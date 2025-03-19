using XerifeTv.CMS.Models.Abstractions;
using XerifeTv.CMS.Models.User.Dtos.Request;
using XerifeTv.CMS.Models.User.Dtos.Response;

namespace XerifeTv.CMS.Models.User.Interfaces;

public interface IUserService
{
  Task<Result<string>> Register(RegisterUserRequestDto dto);
  Task<Result<LoginUserResponseDto>> Login(LoginUserRequestDto dto);
  Task<Result<PagedList<GetUserRequestDto>>> Get(int currentPage, int limit);
  Task<Result<bool>> Delete(string id);
  Task<Result<(string? newToken, string? newRefreshToken)>> TryRefreshSession(string refreshToken);
}
