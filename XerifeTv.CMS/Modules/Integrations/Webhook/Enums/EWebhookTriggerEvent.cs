namespace XerifeTv.CMS.Modules.Integrations.Webhook.Enums;

public enum EWebhookTriggerEvent
{
    MOVIE_PUBLISHED = 1,
    SERIES_PUBLISHED = 2,
    EPISODE_PUBLISHED = 3,
    CHANNEL_PUBLISHED = 4
}

public static class EWebhookTriggerEventExtensions
{
    public static string GetDescription(this EWebhookTriggerEvent eventType)
    {
        return eventType switch
        {
            EWebhookTriggerEvent.MOVIE_PUBLISHED => "Filme Publicado",
            EWebhookTriggerEvent.SERIES_PUBLISHED => "Serie Publicada",
            EWebhookTriggerEvent.EPISODE_PUBLISHED => "Episodio de uma Serie Publicado",
            EWebhookTriggerEvent.CHANNEL_PUBLISHED => "Canal Publicado",
            _ => "Unknown Event"
        };
    }

    public static string[] GetKeywords(this EWebhookTriggerEvent eventType)
    {
        switch (eventType)
        {
            case EWebhookTriggerEvent.MOVIE_PUBLISHED:
                return [
                    "{{ImdbId}}",
                    "{{Titulo}}",
                    "{{PosterUrl}}",
                    "{{BannerUrl}}",
                    "{{Ano Lancamento}}",
                    "{{Classificacao Indicativa}}",
                    "{{Tempo}}"
                ];

            case EWebhookTriggerEvent.SERIES_PUBLISHED:
                return [
                    "{{ImdbId}}",
                    "{{Titulo}}",
                    "{{PosterUrl}}",
                    "{{BannerUrl}}",
                    "{{Ano Lancamento}}",
                    "{{Classificacao Indicativa}}",
                    "{{Temporadas}}"
                ];

            case EWebhookTriggerEvent.EPISODE_PUBLISHED:
                return [
                    "{{Serie ImdbId}}",
                    "{{Serie Titulo}}",
                    "{{Temporada Numero}}",
                    "{{Episodio Numero}}",
                    "{{Episodio Titulo}}",
                    "{{Episodio BannerUrl}}",
                    "{{Serie PosterUrl}}",
                ];

            case EWebhookTriggerEvent.CHANNEL_PUBLISHED:
                return [
                    "{{Titulo}}",
                    "{{LogoUrl}}"
                ];

            default:
                return [];
        }
    }
}