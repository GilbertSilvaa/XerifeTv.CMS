using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.User.Dtos.Request;
using XerifeTv.CMS.Modules.User.Dtos.Response;

namespace XerifeTv.CMS.Modules.User.Interfaces;

public interface IUserService
{
  Task<Result<string>> Register(RegisterUserRequestDto dto);
  Task<Result<LoginUserResponseDto>> Login(LoginUserRequestDto dto);
  Task<Result<PagedList<GetUserResponseDto>>> Get(int currentPage, int limit);
  Task<Result<GetUserResponseDto?>> GetByUsername(string userName);
  Task<Result<string>> Update(UpdateUserRequestDto dto);
  Task<Result<string>> UpdatePassword(UpdatePasswordUserRequestDto dto);
  Task<Result<bool>> Delete(string id);
  Task<Result<(string? newToken, string? newRefreshToken)>> TryRefreshSession(string refreshToken);
  Task<Result<string>> SendEmailResetPassword(string email);
}
