namespace XerifeTv.CMS.Modules.User.Dtos.Request;

public record LoginUserRequestDto(
  string UserNameOrEmail,
  string Password);
