using XerifeTv.CMS.Modules.Authentication.Dtos.Request;
using XerifeTv.CMS.Modules.Authentication.Dtos.Response;
using XerifeTv.CMS.Modules.Authentication.Enums;
using XerifeTv.CMS.Modules.Authentication.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.User.Interfaces;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Modules.Authentication.Services;

public class BasicLoginStrategy(IUserService userService, ITokenService tokenService) : ILoginStrategy
{
	public async Task<Result<LoginResponseDto>> AuthenticateAsync(LoginRequestDto dto)
	{
		try
		{
			var response = RegexHelper.IsValidEmail(dto.UserNameOrEmail)
				? await userService.GetByEmailAsync(dto.UserNameOrEmail)
				: await userService.GetByUsernameAsync(dto.UserNameOrEmail);

			if (response.IsFailure)
				return Result<LoginResponseDto>.Failure(response.Error);

			var userResult = response.Data!;

			if (userResult.Blocked)
				return Result<LoginResponseDto>.Failure(new Error("403", "Usuário bloqueado"));

			var isPasswordCorrectResponse = await userService.IsPasswordCorrect(userResult.Id, dto.Password);

			if (isPasswordCorrectResponse.IsFailure)
				return Result<LoginResponseDto>.Failure(isPasswordCorrectResponse.Error);

			if (!isPasswordCorrectResponse.Data)
				return Result<LoginResponseDto>.Failure(new Error("401", "Credênciais inválidas"));

			return Result<LoginResponseDto>.Success(
				new LoginResponseDto(
					tokenService.GenerateToken(userResult.UserName, userResult.Role),
					tokenService.GenerateRefreshToken(userResult.UserName)));
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

