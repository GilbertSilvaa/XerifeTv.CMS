namespace XerifeTv.CMS.Modules.User.Dtos.Request;

public record ResetPasswordRequestDto(string Id, string Password, string ConfirmPassword, string CodeGuid);