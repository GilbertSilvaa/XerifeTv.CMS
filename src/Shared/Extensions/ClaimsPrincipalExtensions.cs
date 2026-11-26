using System.Security.Claims;

namespace XerifeTv.CMS.Shared.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static bool IsVisitorLike(this ClaimsPrincipal user)
        => user.IsInRole("visitor") || user.IsInRole("demo");

    public static bool IsDemo(this ClaimsPrincipal user)
        => user.IsInRole("demo");

    public static bool IsAdminOrCommon(this ClaimsPrincipal user)
        => user.IsInRole("admin") || user.IsInRole("common");
}
