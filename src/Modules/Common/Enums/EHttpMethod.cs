namespace XerifeTv.CMS.Modules.Common.Enums;

public enum EHttpMethod
{
    POST = 1,
    GET = 2,
    PUT = 3,
    PATCH = 4,
    DELETE = 5
}

public static class EHttpMethodExtensions
{
    public static HttpMethod ToHttpMethod(this EHttpMethod method) => method switch
    {
        EHttpMethod.POST => HttpMethod.Post,
        EHttpMethod.GET => HttpMethod.Get,
        EHttpMethod.PUT => HttpMethod.Put,
        EHttpMethod.PATCH => HttpMethod.Patch,
        EHttpMethod.DELETE => HttpMethod.Delete,
        _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
    };

    public static bool IsBodySupported(this EHttpMethod method) => method switch
    {
        EHttpMethod.POST => true,
        EHttpMethod.PUT => true,
        EHttpMethod.PATCH => true,
        EHttpMethod.GET => false,
        EHttpMethod.DELETE => false,
        _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
    };
}