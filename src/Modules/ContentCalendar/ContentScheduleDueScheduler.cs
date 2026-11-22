using System.Text.Json;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.BackgroundJobQueue;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;
using XerifeTv.CMS.Modules.Common.Enums;
using XerifeTv.CMS.Modules.ContentCalendar;
using XerifeTv.CMS.Modules.ContentCalendar.Entities;
using XerifeTv.CMS.Modules.ContentCalendar.Enums;
using XerifeTv.CMS.Modules.ContentCalendar.Interfaces;
using XerifeTv.CMS.Modules.ContentCalendar.Services;
using XerifeTv.CMS.Modules.Integrations.Webhook.Entities;
using XerifeTv.CMS.Modules.Integrations.Webhook.Enums;
using XerifeTv.CMS.Modules.Integrations.Webhook.Interfaces;

namespace XerifeTv.CMS.Modules.ContentCalendar;

public sealed class ContentScheduleDueScheduler(
    IServiceProvider serviceProvider,
    ICacheService cacheService,
    IConfiguration configuration,
    ILogger<ContentScheduleDueScheduler> logger) : BackgroundService
{
    private const string DebounceKey = "content-schedules:due-scan:running";
    private static readonly TimeSpan ClaimLease = TimeSpan.FromMinutes(30);
    private const int BatchSize = 100;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var minutes = Math.Max(1, configuration.GetValue<int?>("ContentSchedule:DueScanIntervalMinutes") ?? 5);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(minutes));

        do
        {
            await RunScanAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunScanAsync(CancellationToken cancellationToken)
    {
        // Debounce: avoid overlapping scans.
        if (await cacheService.GetValueAsync<bool>(DebounceKey))
            return;

        await cacheService.SetValueAsync(DebounceKey, TimeSpan.FromMinutes(10), true);

        try
        {
            using var scope = serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IContentScheduleRepository>();
            var clock = scope.ServiceProvider.GetRequiredService<IBusinessClock>();
            var webhookService = scope.ServiceProvider.GetRequiredService<IWebhookService>();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IWebhookDispatcher>();

            await repository.ReleaseStaleClaimsAsync(ClaimLease);

            var schedules = await repository.GetDueUnnotifiedAsync(clock.Today, BatchSize);

            foreach (var schedule in schedules)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var claimed = await repository.TryClaimAsync(schedule.Id, ClaimLease);
                if (claimed is null)
                    continue;

                try
                {
                    var webhooksResult = await webhookService.GetByTriggerEventAsync(
                        EWebhookTriggerEvent.CONTENT_SCHEDULE_DUE);

                    var webhooks = (webhooksResult.IsSuccess ? webhooksResult.Data : null)
                        ?.Where(w => !w.IsDisabled)
                        .ToList() ?? [];

                    var payload = BuildPayload(claimed);

                    foreach (var webhook in webhooks)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        string? body = webhook.HttpMethod.IsBodySupported()
                            ? (string.IsNullOrWhiteSpace(webhook.PayloadTemplate)
                                ? payload
                                : EWebhookTriggerEvent.CONTENT_SCHEDULE_DUE.ReplaceKeywords(
                                    webhook.PayloadTemplate, claimed))
                            : null;

                        await dispatcher.DispatchAsync(
                            webhook,
                            EWebhookTriggerEvent.CONTENT_SCHEDULE_DUE,
                            claimed.Id,
                            body,
                            cancellationToken);
                    }

                    // Mark as processed regardless of delivery outcome: at most one event per schedule.
                    await repository.MarkProcessedAsync(claimed.Id);

                    logger.LogInformation(
                        "Content schedule {ScheduleId} processed for due notification ({WebhookCount} webhooks).",
                        claimed.Id, webhooks.Count);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to process due content schedule {ScheduleId}", claimed.Id);
                    await repository.ReleaseClaimAsync(claimed.Id);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running content schedule due scan.");
        }
        finally
        {
            await cacheService.RemoveAsync(DebounceKey);
        }
    }

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
