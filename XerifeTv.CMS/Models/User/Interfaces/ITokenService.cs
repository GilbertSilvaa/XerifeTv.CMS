namespace XerifeTv.CMS.Models.User.Interfaces;

public interface ITokenService
{
  string GenerateToken(UserEntity user);
  string GenerateRefreshToken(UserEntity user);
  
  Task<(bool isValid, string? userName)> ValidateTokenAsync(string token);
}
