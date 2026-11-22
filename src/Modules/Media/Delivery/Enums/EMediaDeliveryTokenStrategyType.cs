namespace XerifeTv.CMS.Modules.Media.Delivery.Enums;

public enum EMediaDeliveryTokenStrategyType
{
    NONE = 0,
    STATIC_QUERY_PARAMETERS = 1,
    SIGNED_QUERY_PARAMETERS = 2
}

public static class EMediaDeliveryTokenStrategyTypeExtensions
{
    public static string ToDescriptionString(this EMediaDeliveryTokenStrategyType type)
    {
        return type switch
        {
            EMediaDeliveryTokenStrategyType.NONE => "Nenhum token ou parametro de segurança",
            EMediaDeliveryTokenStrategyType.STATIC_QUERY_PARAMETERS => "Parametros fixos na query string",
            EMediaDeliveryTokenStrategyType.SIGNED_QUERY_PARAMETERS => "Parametros dinâmicos na query string",
            _ => "Unknown"
        };
    }
}