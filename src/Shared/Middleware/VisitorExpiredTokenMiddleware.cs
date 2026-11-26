using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using XerifeTv.CMS.Modules.Authentication.Services;

namespace XerifeTv.CMS.Shared.Middleware;

public sealed class VisitorExpiredTokenMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (IsResolveUrlRequest(context.Request.Path) &&
            context.User.Identity?.IsAuthenticated != true)
        {
            var token = GetToken(context.Request);

            if (!string.IsNullOrWhiteSpace(token))
            {
                var principal = ValidateExpiredVisitorToken(token);

                if (principal is not null)
                    context.User = principal;
            }
        }

        await next(context);
    }

    private static bool IsResolveUrlRequest(PathString path)
        => path.Value?.Contains("ResolveUrl", StringComparison.OrdinalIgnoreCase) == true;

    private static string? GetToken(HttpRequest request)
    {
        var cookieToken = request.Cookies["Token"] ?? request.Cookies["token"];

        if (!string.IsNullOrWhiteSpace(cookieToken))
            return cookieToken;

        var authorization = request.Headers.Authorization.ToString();

        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return authorization["Bearer ".Length..].Trim();

        return null;
    }

    private ClaimsPrincipal? ValidateExpiredVisitorToken(string token)
    {
        try
        {
            var validationParameters = TokenService.GetTokenValidationParameters(configuration);
            validationParameters.ValidateLifetime = false;

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken)
                return null;

            var expiration = jwtToken.Payload.Expiration;

            if (expiration is null)
                return null;

            if (DateTimeOffset.FromUnixTimeSeconds(expiration.Value) >= DateTimeOffset.UtcNow)
                return null;

            var role = principal.FindFirst(ClaimTypes.Role)?.Value;

            if (!string.Equals(role, "visitor", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(role, "demo", StringComparison.OrdinalIgnoreCase))
                return null;

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
