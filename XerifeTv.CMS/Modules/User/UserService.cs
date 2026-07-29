using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.User.Dtos.Request;
using XerifeTv.CMS.Modules.User.Dtos.Response;
using XerifeTv.CMS.Modules.User.Interfaces;
using XerifeTv.CMS.Modules.User.Specifications;

namespace XerifeTv.CMS.Modules.User;

public sealed class UserService(
  IHashPassword hashPassword,
  IUserRepository repository,
  IEmailService emailService) : IUserService
{
	public async Task<Result<PagedList<GetUserResponseDto>>> GetAsync(int currentPage, int limit, bool includeAdmin = false)
	{
		try
		{
			var response = await repository.GetAsync(currentPage, limit);

			var result = new PagedList<GetUserResponseDto>(
				response.CurrentPage,
				response.TotalPageCount,
				response.Items
					.Where(r => (r.Role != Enums.EUserRole.ADMIN || includeAdmin))
					.Select(GetUserResponseDto.FromEntity));

			return Result<PagedList<GetUserResponseDto>>.Success(result);
		}
		catch (Exception ex)
		{
			var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
			return Result<PagedList<GetUserResponseDto>>.Failure(error);
		}
	}

	public async Task<Result<GetUserResponseDto?>> GetByUsernameAsync(string username)
	{
		try
		{
			var response = await repository.GetByUsernameAsync(username);

			if (response == null)
				return Result<GetUserResponseDto?>.Failure(
				  new Error("404", "Usuário não encontrado"));

			return Result<GetUserResponseDto?>.Success(GetUserResponseDto.FromEntity(response));
		}
		catch (Exception ex)
		{
			var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
			return Result<GetUserResponseDto?>.Failure(error);
		}
	}

    public async Task<Result<GetUserResponseDto?>> GetByEmailAsync(string email)
    {
        try
        {
            var response = await repository.GetByEmailAsync(email);

            if (response == null)
                return Result<GetUserResponseDto?>.Failure(
                  new Error("404", "Usuário não encontrado"));

            return Result<GetUserResponseDto?>.Success(GetUserResponseDto.FromEntity(response));
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<GetUserResponseDto?>.Failure(error);
        }
    }

    public async Task<Result<string>> RegisterAsync(RegisterUserRequestDto dto)
	{
		try
		{
			var entity = dto.ToEntity();
			var emailSpec = new UniqueEmailSpecification(repository);
			var usernameSpec = new UniqueUsernameSpecification(repository);

			if (!await emailSpec.IsSatisfiedByAsync(entity))
				return Result<string>.Failure(new Error("409", "Email já está em uso"));

			if (!await usernameSpec.IsSatisfiedByAsync(entity))
				return Result<string>.Failure(new Error("409", "Username já está em uso"));

			entity.Password = hashPassword.Encrypt(dto.Password);
			var response = await repository.CreateAsync(entity);

			return Result<string>.Success(response);
		}
		catch (ArgumentException ex)
		{
			return Result<string>.Failure(new Error("400", ex.Message));
		}
		catch (Exception ex)
		{
			var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
			return Result<string>.Failure(error);
		}
	}

	public async Task<Result<string>> UpdateAsync(UpdateUserRequestDto dto)
	{
		try
		{
			var user = await repository.GetAsync(dto.Id);

			if (user == null)
				return Result<string>.Failure(new Error("404", "Usuário não encontrado"));

			var emailSpec = new UniqueEmailSpecification(repository);
			var usernameSpec = new UniqueUsernameSpecification(repository);

			if (!await emailSpec.IsSatisfiedByAsync(dto.ToEntity()))
				return Result<string>.Failure(new Error("409", "Email já está em uso"));

			if (!await usernameSpec.IsSatisfiedByAsync(dto.ToEntity()))
				return Result<string>.Failure(new Error("409", "Username já está em uso"));

			if (dto.Blocked == false && user.Blocked)
				user.FailedLoginAttempts = 0;

			user.Email = dto.Email ?? user.Email;
			user.UserName = dto.UserName ?? user.UserName;
			user.Role = dto.Role ?? user.Role;
			user.Blocked = dto.Blocked ?? user.Blocked;

			await repository.UpdateAsync(user);

			return Result<string>.Success(user.Id);
		}
		catch (ArgumentException ex)
		{
			return Result<string>.Failure(new Error("400", ex.Message));
		}
		catch (Exception ex)
		{
			var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
			return Result<string>.Failure(error);
		}
	}

	public async Task<Result<string>> UpdatePasswordAsync(UpdatePasswordUserRequestDto dto)
	{
		try
		{
			var user = await repository.GetAsync(dto.Id);

			if (user is null)
				return Result<string>.Failure(new Error("404", "Usuário não encontrado"));

			var isPasswordCorrect = hashPassword.Verify(dto.OldPassword, user.Password);

			if (!isPasswordCorrect)
				return Result<string>.Failure(new Error("401", "Senha atual incorreta"));

			user.Password = hashPassword.Encrypt(dto.NewPassword);
			await repository.UpdateAsync(user);

			return Result<string>.Success(user.Id);
		}
		catch (Exception ex)
		{
			var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
			return Result<string>.Failure(error);
		}
	}

	public async Task<Result<ValidateResetPasswordGuidResponseDto>> ResetPasswordAsync(ResetPasswordRequestDto dto)
	{
		try
		{
			var user = await repository.GetAsync(dto.Id);

			if (user is null)
				return Result<ValidateResetPasswordGuidResponseDto>.Failure(
					new Error("404", "Usuário não encontrado"));

			user.Password = hashPassword.Encrypt(dto.Password);
			user.ResetPasswordGuid = Guid.Empty;
			await repository.UpdateAsync(user);

			var response = new ValidateResetPasswordGuidResponseDto(user.Id, user.Email, dto.CodeGuid);
			return Result<ValidateResetPasswordGuidResponseDto>.Success(response);
		}
		catch (Exception ex)
		{
			var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
			return Result<ValidateResetPasswordGuidResponseDto>.Failure(error);
		}
	}

	public async Task<Result<bool>> DeleteAsync(string id)
	{
		try
		{
			var response = await repository.GetAsync(id);

			if (response is null)
				return Result<bool>.Failure(new Error("404", "Usuário não encontrado"));

			if (response.Role == Enums.EUserRole.ADMIN)
				return Result<bool>.Failure(new Error("403", "Usuário não pode ser deletado"));

			await repository.DeleteAsync(id);
			return Result<bool>.Success(true);
		}
		catch (Exception ex)
		{
			var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
			return Result<bool>.Failure(error);
		}
	}

	public async Task<Result<string>> SendEmailResetPasswordAsync(string email)
	{
		try
		{
			var user = await repository.GetByEmailAsync(email);

			if (user is null)
				return Result<string>.Failure(new Error("404", "Email não encontrado"));

			user.ResetPasswordGuid = Guid.NewGuid();
			user.ResetPasswordGuidExpires = DateTimeOffset.UtcNow.AddMinutes(10);

			await repository.UpdateAsync(user);
			await emailService.SendEmailResetPasswordAsync(email, user.ResetPasswordGuid?.ToString() ?? string.Empty);

			return Result<string>.Success(email);
		}
		catch (Exception ex)
		{
			var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
			return Result<string>.Failure(error);
		}
	}

	public async Task<Result<ValidateResetPasswordGuidResponseDto>> ValidateResetPasswordGuidAsync(Guid guid)
	{
		try
		{
			var user = await repository.GetByResetPasswordGuidAsync(guid);

			if (user is null)
				return Result<ValidateResetPasswordGuidResponseDto>.Failure(new Error("404", "Link inválido"));

			if (user.ResetPasswordGuidExpires < DateTimeOffset.UtcNow)
				return Result<ValidateResetPasswordGuidResponseDto>.Failure(new Error("401", "O link expirou"));

			var response = new ValidateResetPasswordGuidResponseDto(user.Id, user.Email, guid.ToString());
			return Result<ValidateResetPasswordGuidResponseDto>.Success(response);
		}
		catch (Exception ex)
		{
			var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
			return Result<ValidateResetPasswordGuidResponseDto>.Failure(error);
		}
	}

    public async Task<Result<bool>> IsPasswordCorrect(string userId, string password)
    {
		const int MAX_FAILED_LOGIN_ATTEMPS = 5;

        try
        {
            var user = await repository.GetAsync(userId);

            if (user == null)
                return Result<bool>.Failure(new Error("404", "Usuário não encontrado"));

			if (!hashPassword.Verify(password, user.Password))
			{
				user.FailedLoginAttempts++;

				if (user.FailedLoginAttempts >= MAX_FAILED_LOGIN_ATTEMPS)
					user.Blocked = true;

				await repository.UpdateAsync(user);

				return Result<bool>.Success(false);
			}

			user.FailedLoginAttempts = 0;
            await repository.UpdateAsync(user);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<bool>.Failure(error);
        }
    }
}

