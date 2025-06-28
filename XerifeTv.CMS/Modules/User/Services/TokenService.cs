using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using XerifeTv.CMS.Modules.User;
using XerifeTv.CMS.Modules.User.Interfaces;

namespace XerifeTv.CMS.Modules.User.Services;

public sealed class TokenService(IConfiguration _configuration) : ITokenService
{
    public string GenerateToken(UserEntity user)
    {
        var key = _configuration["Jwt:Key"] ?? string.Empty;
        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        var signInCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

        var tokenClaims = new[]
        {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, user.Role.ToString().ToLower()),
        };

        _ = int.TryParse(_configuration["Jwt:ExpirationTimeInMinutes"], out int expireTimeInMinutes);

        var tokenOptions = new JwtSecurityToken(
            issuer,
            audience,
            tokenClaims,
            signingCredentials: signInCredentials,
            expires: DateTime.UtcNow.AddMinutes(expireTimeInMinutes));

        return new JwtSecurityTokenHandler().WriteToken(tokenOptions);
    }

    public string GenerateRefreshToken(UserEntity user)
    {
        var key = _configuration["Jwt:Key"] ?? string.Empty;
        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        var signInCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);
        var tokenClaims = new[] { new Claim(ClaimTypes.Name, user.UserName) };

        _ = int.TryParse(_configuration["Jwt:RefreshExpirationTimeInMinutes"], out var expireTimeInMinutes);

        var tokenOptions = new JwtSecurityToken(
            issuer,
            audience,
            tokenClaims,
            signingCredentials: signInCredentials,
            expires: DateTime.UtcNow.AddMinutes(expireTimeInMinutes));

        return new JwtSecurityTokenHandler().WriteToken(tokenOptions);
    }

    public async Task<(bool isValid, string? userName)> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return (false, null);

        var tokenValidationParams = GetTokenValidationParameters(_configuration);
        var validTokenResult = await new JwtSecurityTokenHandler().ValidateTokenAsync(token, tokenValidationParams);

        if (!validTokenResult.IsValid)
            return (false, null);

        var userName = validTokenResult.Claims
          .FirstOrDefault(x => x.Key == ClaimTypes.Name).Value as string;

        return (true, userName);
    }

    public static TokenValidationParameters GetTokenValidationParameters(IConfiguration _configuration)
    {
        var tokenKey = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? string.Empty);

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidAudience = _configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(tokenKey),
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    }
}
