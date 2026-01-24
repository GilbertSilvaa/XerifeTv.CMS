using System.Security.Cryptography;
using System.Text;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Media.Delivery.Enums;
using XerifeTv.CMS.Modules.Media.Delivery.Intefaces;

public class MediaDeliveryTokenSignedQueryStrategy : IMediaDeliveryTokenStrategy
{
    public bool CanHandle(EMediaDeliveryTokenStrategyType tokenStrategyType)
        => tokenStrategyType == EMediaDeliveryTokenStrategyType.SIGNED_QUERY_PARAMETERS;

    public Result<string> Resolve(Dictionary<string, string> @params)
    {
        try
        {
            if (@params == null)
                return Result<string>.Failure(new Error("400", "Parameters not provided"));

            if (!@params.TryGetValue("secret", out var secret))
                return Result<string>.Failure(new Error("400", "Secret not provided"));

            if (!@params.TryGetValue("expiresIn", out var expiresInRaw) || !int.TryParse(expiresInRaw, out var expiresIn))
                return Result<string>.Failure(new Error("400", "Invalid expiresIn"));

            var expires = DateTimeOffset.UtcNow
                .AddSeconds(expiresIn)
                .ToUnixTimeSeconds()
                .ToString();

            @params.Remove("secret");
            @params.Remove("expiresIn");

            var payload = string.Join("&", @params.Select(p => $"{p.Key}={p.Value}"));

            var dataToSign = $"{payload}&expires={expires}";
            var signature = Sign(dataToSign, secret);

            var query = $"{dataToSign}&signature={signature}";

            return Result<string>.Success(query);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<string>.Failure(error);
        }
    }

    private static string Sign(string data, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
