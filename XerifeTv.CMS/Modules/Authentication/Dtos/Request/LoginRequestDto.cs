using XerifeTv.CMS.Modules.Authentication.Enums;

namespace XerifeTv.CMS.Modules.Authentication.Dtos.Request;

public record LoginRequestDto(
    string UserNameOrEmail,
    string Password,
    ELoginProvider Provider,
    string ExternalToken);