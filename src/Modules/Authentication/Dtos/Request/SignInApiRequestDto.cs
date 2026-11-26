using XerifeTv.CMS.Modules.Authentication.Enums;

namespace XerifeTv.CMS.Modules.Authentication.Dtos.Request;

public sealed class SignInApiRequestDto
{
    public string UserNameOrEmail { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public ELoginProvider Provider { get; init; } = ELoginProvider.Basic;
    public string ExternalToken { get; init; } = string.Empty;

    public LoginRequestDto ToLoginRequestDto()
        => new(UserNameOrEmail, Password, Provider, ExternalToken);
}
