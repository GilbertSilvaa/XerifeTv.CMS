namespace XerifeTv.CMS.Modules.User.Enums;

public static class EUserRoleExtensions
{
    public static bool IsVisitorLike(this EUserRole role)
        => role is EUserRole.VISITOR or EUserRole.DEMO;

    public static bool IsAdminOrCommon(this EUserRole role)
        => role is EUserRole.ADMIN or EUserRole.COMMON;
}
