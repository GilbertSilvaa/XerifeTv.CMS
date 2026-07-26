using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.Channel;
using XerifeTv.CMS.Modules.Movie;
using XerifeTv.CMS.Modules.Series;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Modules.Integrations.Webhook.Enums;

public enum EWebhookTriggerEvent
{
    MOVIE_PUBLISHED = 1,
    SERIES_PUBLISHED = 2,
    CHANNEL_PUBLISHED = 4
}

public static class EWebhookTriggerEventExtensions
{
    public static string GetDescription(this EWebhookTriggerEvent eventType)
    {
        return eventType switch
        {
            EWebhookTriggerEvent.MOVIE_PUBLISHED => "Filme Publicado",
            EWebhookTriggerEvent.SERIES_PUBLISHED => "Série Publicada",
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
                    "{{Id}}",
                    "{{ImdbId}}",
                    "{{Título}}",
                    "{{PosterUrl}}",
                    "{{BannerUrl}}",
                    "{{Ano Lançamento}}",
                    "{{Classificação Indicativa}}",
                    "{{Tempo}}"
                ];

            case EWebhookTriggerEvent.SERIES_PUBLISHED:
                return [
                    "{{Id}}",
                    "{{ImdbId}}",
                    "{{Título}}",
                    "{{PosterUrl}}",
                    "{{BannerUrl}}",
                    "{{Ano Lançamento}}",
                    "{{Classificação Indicativa}}",
                    "{{Temporadas}}"
                ];

            case EWebhookTriggerEvent.CHANNEL_PUBLISHED:
                return [
                    "{{Id}}",
                    "{{Título}}",
                    "{{LogoUrl}}"
                ];

            default:
                return [];
        }
    }

    public static string ReplaceKeywords<T>(this EWebhookTriggerEvent eventType, string payloadTemplate, T entity) where T : BaseEntity
    {
        if (eventType == EWebhookTriggerEvent.MOVIE_PUBLISHED && entity is MovieEntity movieEntity)
        {
            payloadTemplate = payloadTemplate.Replace("{{Id}}", movieEntity!.Id);
            payloadTemplate = payloadTemplate.Replace("{{ImdbId}}", movieEntity!.ImdbId);
            payloadTemplate = payloadTemplate.Replace("{{Título}}", movieEntity!.Title);
            payloadTemplate = payloadTemplate.Replace("{{Titulo}}", movieEntity!.Title);
            payloadTemplate = payloadTemplate.Replace("{{PosterUrl}}", movieEntity!.PosterUrl);
            payloadTemplate = payloadTemplate.Replace("{{BannerUrl}}", movieEntity!.BannerUrl);
            payloadTemplate = payloadTemplate.Replace("{{Ano Lançamento}}", movieEntity!.ReleaseYear.ToString());
            payloadTemplate = payloadTemplate.Replace("{{Ano Lancamento}}", movieEntity!.ReleaseYear.ToString());
            payloadTemplate = payloadTemplate.Replace("{{Classificação Indicativa}}", movieEntity!.ParentalRating.ToString());
            payloadTemplate = payloadTemplate.Replace("{{Classificacao Indicativa}}", movieEntity!.ParentalRating.ToString());
            payloadTemplate = payloadTemplate.Replace("{{Tempo}}", DateTimeHelper.ConvertSecondsToHHmm(movieEntity!.Video!.Duration));
        }

        if (eventType == EWebhookTriggerEvent.SERIES_PUBLISHED && entity is SeriesEntity seriesEntity)
        {
            payloadTemplate = payloadTemplate.Replace("{{Id}}", seriesEntity!.Id);
            payloadTemplate = payloadTemplate.Replace("{{ImdbId}}", seriesEntity!.ImdbId);
            payloadTemplate = payloadTemplate.Replace("{{Título}}", seriesEntity!.Title);
            payloadTemplate = payloadTemplate.Replace("{{Titulo}}", seriesEntity!.Title);
            payloadTemplate = payloadTemplate.Replace("{{PosterUrl}}", seriesEntity!.PosterUrl);
            payloadTemplate = payloadTemplate.Replace("{{BannerUrl}}", seriesEntity!.BannerUrl);
            payloadTemplate = payloadTemplate.Replace("{{Ano Lançamento}}", seriesEntity!.ReleaseYear.ToString());
            payloadTemplate = payloadTemplate.Replace("{{Ano Lancamento}}", seriesEntity!.ReleaseYear.ToString());
            payloadTemplate = payloadTemplate.Replace("{{Classificação Indicativa}}", seriesEntity!.ParentalRating.ToString());
            payloadTemplate = payloadTemplate.Replace("{{Classificacao Indicativa}}", seriesEntity!.ParentalRating.ToString());
            payloadTemplate = payloadTemplate.Replace("{{Temporadas}}", seriesEntity!.NumberSeasons.ToString());
        }

        if (eventType == EWebhookTriggerEvent.CHANNEL_PUBLISHED && entity is ChannelEntity channelEntity)
        {
            payloadTemplate = payloadTemplate.Replace("{{Id}}", channelEntity!.Id);
            payloadTemplate = payloadTemplate.Replace("{{Título}}", channelEntity!.Title);
            payloadTemplate = payloadTemplate.Replace("{{Titulo}}", channelEntity!.Title);
            payloadTemplate = payloadTemplate.Replace("{{LogoUrl}}", channelEntity!.LogoUrl);
        }

        return payloadTemplate;
    }
}
