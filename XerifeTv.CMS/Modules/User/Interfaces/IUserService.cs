using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.User.Dtos.Request;
using XerifeTv.CMS.Modules.User.Dtos.Response;

namespace XerifeTv.CMS.Modules.User.Interfaces;

public interface IUserService
{
    Task<Result<string>> RegisterAsync(RegisterUserRequestDto dto);
    Task<Result<PagedList<GetUserResponseDto>>> GetAsync(int currentPage, int limit, bool includeAdmin = false);
    Task<Result<GetUserResponseDto?>> GetByUsernameAsync(string userName);
    Task<Result<GetUserResponseDto?>> GetByEmailAsync(string email);
    Task<Result<string>> UpdateAsync(UpdateUserRequestDto dto);
    Task<Result<string>> UpdatePasswordAsync(UpdatePasswordUserRequestDto dto);
    Task<Result<ValidateResetPasswordGuidResponseDto>> ResetPasswordAsync(ResetPasswordRequestDto dto);
    Task<Result<bool>> DeleteAsync(string id);
    Task<Result<string>> SendEmailResetPasswordAsync(string email);
    Task<Result<ValidateResetPasswordGuidResponseDto>> ValidateResetPasswordGuidAsync(Guid guid);
    Task<Result<bool>> IsPasswordCorrect(string userId, string password);
}
