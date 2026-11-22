using System.Text.Json;
using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Response;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;
using XerifeTv.CMS.Modules.Common.Enums;
using XerifeTv.CMS.Modules.ContentCalendar;
using XerifeTv.CMS.Modules.ContentCalendar.Entities;
using XerifeTv.CMS.Modules.ContentCalendar.Enums;
using XerifeTv.CMS.Modules.ContentCalendar.Interfaces;
using XerifeTv.CMS.Modules.Integrations.Webhook.Entities;
using XerifeTv.CMS.Modules.Integrations.Webhook.Enums;
using XerifeTv.CMS.Modules.Integrations.Webhook.Interfaces;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue.ProcessorStrategies;

public sealed class DispatchContentScheduleWebhookProcessorStrategy(
    IServiceProvider serviceProvider,
    IWebhookDispatcher webhookDispatcher,
    ILogger<DispatchContentScheduleWebhookProcessorStrategy> logger) : IBackgroundJobProcessorStrategy
{
    public async Task ProcessJobAsync(GetBackgroundJobResponseDto job, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IContentScheduleRepository>();
        var webhookService = scope.ServiceProvider.GetRequiredService<IWebhookService>();
        var queue = scope.ServiceProvider.GetRequiredService<IBackgroundJobQueueService>();

        var schedule = await repository.GetAsync(job.DispatchWebhooksEntityId!);
        if (schedule is null)
        {
            await queue.UpdateAsync(new UpdateBackgroundJobRequestDto
            {
                Id = job.Id,
                Status = EBackgroundJobStatus.FAILED,
                TotalRecordsToProcess = 0,
                TotalProcessedRecords = 0,
                ErrorList = ["Agendamento não encontrado."]
            });
            return;
        }

        var webhooksResult = await webhookService.GetByTriggerEventAsync(EWebhookTriggerEvent.CONTENT_SCHEDULE_DUE);

        if (webhooksResult.IsFailure)
        {
            logger.LogError("Failed to retrieve webhooks for content schedule due: {Error}", webhooksResult.Error);
            return;
        }

        var webhooks = webhooksResult.Data!.Where(w => !w.IsDisabled).ToList();

        var update = new UpdateBackgroundJobRequestDto
        {
            Id = job.Id,
            TotalRecordsToProcess = webhooks.Count,
            TotalSuccessfulRecords = 0,
            TotalProcessedRecords = 0,
            Status = EBackgroundJobStatus.PROCESSING
        };

        await queue.UpdateAsync(update);

        var payloadContent = BuildPayload(schedule);

        foreach (var webhook in webhooks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? body = webhook.HttpMethod.IsBodySupported()
                ? (string.IsNullOrWhiteSpace(webhook.PayloadTemplate)
                    ? payloadContent
                    : EWebhookTriggerEvent.CONTENT_SCHEDULE_DUE.ReplaceKeywords(webhook.PayloadTemplate, schedule))
                : null;

            var success = await webhookDispatcher.DispatchAsync(
                webhook, EWebhookTriggerEvent.CONTENT_SCHEDULE_DUE, schedule.Id, body, cancellationToken);

            update = new UpdateBackgroundJobRequestDto
            {
                Id = job.Id,
                TotalRecordsToProcess = update.TotalRecordsToProcess,
                TotalSuccessfulRecords = update.TotalSuccessfulRecords + (success ? 1 : 0),
                TotalProcessedRecords = update.TotalProcessedRecords + 1,
                TotalFailedRecords = update.TotalFailedRecords + (success ? 0 : 1),
                ErrorList = success ? update.ErrorList : [.. update.ErrorList, webhook.Id],
                Status = EBackgroundJobStatus.PROCESSING
            };

            await queue.UpdateAsync(update);
        }

        update.Status = update.TotalFailedRecords > 0
            ? EBackgroundJobStatus.FAILED
            : EBackgroundJobStatus.COMPLETED;

        await queue.UpdateAsync(update);
    }

    public bool CanProcess(EBackgroundJobType jobType)
        => jobType == EBackgroundJobType.DISPATCH_WEBHOOKS_CONTENT_SCHEDULE;

    private static string BuildPayload(ContentScheduleEntity schedule)
    {
        var data = new Dictionary<string, object?>
        {
            ["scheduleId"] = schedule.Id,
            ["contentType"] = ContentScheduleDisplay.GetTypeLabel(schedule.ContentType),
            ["imdbId"] = schedule.ImdbId,
            ["tmdbId"] = schedule.TmdbId,
            ["title"] = schedule.Title,
            ["scheduledDate"] = schedule.ScheduledDate.ToString("yyyy-MM-dd"),
            ["origin"] = ContentScheduleDisplay.GetOriginLabel(schedule.Origin)
        };

        if (schedule.ContentType == EContentScheduleType.EPISODE)
        {
            data["seasonNumber"] = schedule.SeasonNumber;
            data["episodeNumber"] = schedule.EpisodeNumber;
        }

        var payload = new
        {
            @event = "content.schedule.due",
            occurredAt = DateTime.UtcNow.ToString("o"),
            data
        };

        return JsonSerializer.Serialize(payload);
    }
}
