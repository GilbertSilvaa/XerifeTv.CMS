using XerifeTv.CMS.Modules.User;

namespace XerifeTv.CMS.Modules.User.Interfaces;

public interface ITokenService
{
  string GenerateToken(UserEntity user);
  string GenerateRefreshToken(UserEntity user);
  
  Task<(bool isValid, string? userName)> ValidateTokenAsync(string token);
}
