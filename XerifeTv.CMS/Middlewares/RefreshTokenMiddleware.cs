using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using XerifeTv.CMS.Models.User.Interfaces;

namespace XerifeTv.CMS.Middlewares;

public class RefreshTokenMiddleware(RequestDelegate _next, IConfiguration _configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var token = context.Request.Cookies["token"];
        var refreshToken = context.Request.Cookies["refreshToken"];

        using var scope = context.RequestServices.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        
        var (isTokenValid, _) = await tokenService.ValidateTokenAsync(token);
        if (!isTokenValid) await TryRefreshToken(context, tokenService, userRepository, refreshToken);
        
        await _next(context);
    }
    
    private async Task TryRefreshToken(
        HttpContext context,
        ITokenService tokenService,
        IUserRepository userRepository,
        string refreshToken)
    {
        var (isRefreshTokenValid, userName) = await tokenService.ValidateTokenAsync(refreshToken);

        if (isRefreshTokenValid)
        {
            var user = await userRepository.GetByUserNameAsync(userName);
            
            if (user != null)
            {
                var newToken = tokenService.GenerateToken(user);
                var newRefreshToken = tokenService.GenerateRefreshToken(user);

                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddHours(1)
                };
                
                context.Response.Cookies.Append("token", newToken, cookieOptions);
                context.Response.Cookies.Append("refreshToken", newRefreshToken, cookieOptions);
            }
        }
    }
}
