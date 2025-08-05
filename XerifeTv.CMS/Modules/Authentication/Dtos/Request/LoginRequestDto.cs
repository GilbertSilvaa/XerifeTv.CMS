namespace XerifeTv.CMS.Modules.Authentication.Dtos.Request;

public record LoginRequestDto(
    string UserNameOrEmail,
    string Password);