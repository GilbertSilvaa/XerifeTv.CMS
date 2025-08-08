using System.Text.Json;
using XerifeTv.CMS.Modules.Authentication.Dtos.Request;
using XerifeTv.CMS.Modules.Authentication.Dtos.Response;
using XerifeTv.CMS.Modules.Authentication.Enums;
using XerifeTv.CMS.Modules.Authentication.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.User.Interfaces;

namespace XerifeTv.CMS.Modules.Authentication.Services;

public class GoogleLoginStrategy(
	IUserService _userService,
	ITokenService _tokenService,
	IConfiguration _configuration) : ILoginStrategy
{
	public async Task<Result<LoginResponseDto>> AuthenticateAsync(LoginRequestDto dto)
	{
		try
		{
			var httpClient = new HttpClient();
			var response = await httpClient.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={dto.ExternalToken}");

			if (!response.IsSuccessStatusCode)
				return Result<LoginResponseDto>.Failure(new Error("401", "Erro ao validar o token Google"));

			var content = await response.Content.ReadAsStringAsync();
			var payload = JsonSerializer.Deserialize<GoogleTokenPayloadDto>(content);

			if (payload == null)
				return Result<LoginResponseDto>.Failure(new Error("401", "Erro ao validar o token Google"));

			if (payload.Aud != _configuration["OAuth2Google:ClientId"])
				return Result<LoginResponseDto>.Failure(new Error("401", "Token Google invalido: client ID nao autorizado"));

			var expiry = DateTimeOffset.FromUnixTimeSeconds(long.Parse(payload!.Exp));

			if (expiry < DateTimeOffset.UtcNow)
				return Result<LoginResponseDto>.Failure(new Error("401", "Token Google invalido ou expirado"));

			var userResponse = await _userService.GetByEmailAsync(payload!.Email);

			if (userResponse.IsFailure)
				return Result<LoginResponseDto>.Failure(userResponse.Error);

			var userResult = userResponse.Data!;

			if (userResult.Blocked)
				return Result<LoginResponseDto>.Failure(new Error("403", "Usuario bloqueado"));

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
		=> loginProvider == ELoginProvider.Google;
}
