namespace XerifeTv.CMS.Modules.User.Dtos.Response;

public record ValidateResetPasswordGuidResponseDto(string UserId, string UserEmail, string CodeGuid);