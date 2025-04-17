namespace XerifeTv.CMS.Modules.User.Dtos.Request;

public record UpdatePasswordUserRequestDto(
  string Id, 
  string OldPassword, 
  string NewPassword,
  string NewPasswordConfirm);