using XerifeTv.CMS.Modules.Authentication.Dtos.Request;
using XerifeTv.CMS.Modules.Authentication.Dtos.Response;
using XerifeTv.CMS.Modules.Authentication.Enums;
using XerifeTv.CMS.Modules.Authentication.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.User.Interfaces;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Modules.Authentication.Services;

public class BasicLoginStrategy(IUserService _userService, ITokenService _tokenService) : ILoginStrategy
{
	public async Task<Result<LoginResponseDto>> AuthenticateAsync(LoginRequestDto dto)
	{
		try
		{
			var response = RegexHelper.IsValidEmail(dto.UserNameOrEmail)
				? await _userService.GetByEmailAsync(dto.UserNameOrEmail)
				: await _userService.GetByUsernameAsync(dto.UserNameOrEmail);

			if (response.IsFailure)
				return Result<LoginResponseDto>.Failure(response.Error);

			var userResult = response.Data!;

			if (userResult.Blocked)
				return Result<LoginResponseDto>.Failure(new Error("403", "Usuario bloqueado"));

			var isPasswordCorrectResponse = await _userService.IsPasswordCorrect(userResult.Id, dto.Password);

			if (isPasswordCorrectResponse.IsFailure)
				return Result<LoginResponseDto>.Failure(isPasswordCorrectResponse.Error);

			if (!isPasswordCorrectResponse.Data)
				return Result<LoginResponseDto>.Failure(new Error("401", "Credenciais invalidas"));

			return Result<LoginResponseDto>.Success(
				new LoginResponseDto(
					_tokenService.GenerateToken(userResult.UserName, userResult.Role),
					_tokenService.GenerateRefreshToken(userResult.UserName)));
		}
		catch (Exception ex)
		{
			var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
			return Result<LoginResponseDto>.Failure(error);
		}
	}

	public bool CanHandle(ELoginProvider loginProvider)
		=> loginProvider == ELoginProvider.Basic;
}
