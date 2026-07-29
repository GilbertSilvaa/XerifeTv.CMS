using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using XerifeTv.CMS.Modules.Authentication.Interfaces;
using XerifeTv.CMS.Modules.User.Enums;

namespace XerifeTv.CMS.Modules.Authentication.Services;

public class TokenService(IConfiguration configuration) : ITokenService
{
    public string GenerateToken(string username, EUserRole userRole)
    {
        var key = configuration["Jwt:Key"] ?? string.Empty;
        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];

        var signInCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

        var tokenClaims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, userRole.ToString().ToLower()),
        };

        _ = int.TryParse(configuration["Jwt:ExpirationTimeInMinutes"], out int expireTimeInMinutes);

        var tokenOptions = new JwtSecurityToken(
            issuer,
            audience,
            tokenClaims,
            signingCredentials: signInCredentials,
            expires: DateTime.UtcNow.AddMinutes(expireTimeInMinutes));

        return new JwtSecurityTokenHandler().WriteToken(tokenOptions);
    }

    public string GenerateRefreshToken(string username)
    {
        var key = configuration["Jwt:Key"] ?? string.Empty;
        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];

        var signInCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);
        var tokenClaims = new[] { new Claim(ClaimTypes.Name, username) };

        _ = int.TryParse(configuration["Jwt:RefreshExpirationTimeInMinutes"], out var expireTimeInMinutes);

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

        var tokenValidationParams = GetTokenValidationParameters(configuration);
        var validTokenResult = await new JwtSecurityTokenHandler().ValidateTokenAsync(token, tokenValidationParams);

        if (!validTokenResult.IsValid)
            return (false, null);

        var userName = validTokenResult.Claims
          .FirstOrDefault(x => x.Key == ClaimTypes.Name).Value as string;

        return (true, userName);
    }

    public static TokenValidationParameters GetTokenValidationParameters(IConfiguration configuration)
    {
        var tokenKey = Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? string.Empty);

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(tokenKey),
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    }
}

