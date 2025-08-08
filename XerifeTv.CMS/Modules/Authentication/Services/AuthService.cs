using XerifeTv.CMS.Modules.Authentication.Dtos.Request;
using XerifeTv.CMS.Modules.Authentication.Dtos.Response;
using XerifeTv.CMS.Modules.Authentication.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.User.Interfaces;

namespace XerifeTv.CMS.Modules.Authentication.Services;

public class AuthService(
    IUserService _userService,
    ITokenService _tokenService,
	IEnumerable<ILoginStrategy> _loginStrategies) : IAuthService
{
    public async Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto dto)
    {
        try
        {
			var loginStrategy = _loginStrategies.FirstOrDefault(s => s.CanHandle(dto.Provider));

            if (loginStrategy == null)
                return Result<LoginResponseDto>.Failure(new Error("400", "Login provider not supported"));

            return await loginStrategy.AuthenticateAsync(dto);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<LoginResponseDto>.Failure(error);
        }
    }

    public async Task<Result<(string? newToken, string? newRefreshToken)>> TryRefreshSessionAsync(string refreshToken)
    {
        try
        {
            var (isValid, userName) = await _tokenService.ValidateTokenAsync(refreshToken);

            if (!isValid)
                return Result<(string?, string?)>.Failure(new Error("401", "Token invalido"));

            var response = await _userService.GetByUsernameAsync(userName!);

            if (response.IsFailure)
                return Result<(string? newToken, string? newRefreshToken)>.Failure(response.Error);

            var userResult = response.Data!;

            if (userResult.Blocked)
                return Result<(string?, string?)>.Failure(new Error("403", "Usuario bloqueado"));

            return Result<(string?, string?)>.Success((
                _tokenService.GenerateToken(userResult.UserName, userResult.Role),
                _tokenService.GenerateRefreshToken(userResult.UserName)));
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<(string?, string?)>.Failure(error);
        }
    }
}
