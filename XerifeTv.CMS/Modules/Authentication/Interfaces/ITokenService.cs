using XerifeTv.CMS.Modules.User.Enums;

namespace XerifeTv.CMS.Modules.Authentication.Interfaces;

public interface ITokenService
{
    string GenerateToken(string username, EUserRole userRole);
    string GenerateRefreshToken(string username);

    Task<(bool isValid, string? userName)> ValidateTokenAsync(string token);
}
