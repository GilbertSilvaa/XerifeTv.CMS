
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Abstractions.Services;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;
using XerifeTv.CMS.Modules.Channel.Interfaces;
using XerifeTv.CMS.Modules.Movie.Interfaces;
using XerifeTv.CMS.Modules.Series.Interfaces;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue;

public class BackgroundJobQueueWorker(
    IServiceProvider serviceProvider,
    ICacheService cacheService,
    ILogger<BackgroundJobQueueWorker> logger) : BackgroundService
{
    private const int MaxConcurrentJobs = 2;
    private readonly SemaphoreSlim _semaphore = new(MaxConcurrentJobs);
    private readonly List<string> _processingJobIds = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _semaphore.WaitAsync(stoppingToken);
            await ProcessNextJobAsync(stoppingToken);
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ProcessNextJobAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var backgroundJobQueueService = scope.ServiceProvider.GetRequiredService<IBackgroundJobQueueService>();
            var jobsProcessorStrategies = scope.ServiceProvider.GetServices<IBackgroundJobProcessorStrategy>();

            var filterJobsPendingDto = new GetBackgroundJobsByFilterRequestDto(
                status: EBackgroundJobStatus.PENDING,
                order: EBackgroundJobOrderFilter.REGISTRATION_DATE_ASC,
                limitResults: 1,
                currentPage: 1);

            var pendingJobsResult = await backgroundJobQueueService.GetByFilterAsync(filterJobsPendingDto);

            if (pendingJobsResult.IsFailure)
                throw new Exception(pendingJobsResult.Error.Description);

            var jobQueue = pendingJobsResult?.Data?.Items.FirstOrDefault();

            if (jobQueue == null || _processingJobIds.Contains(jobQueue.Id))
            {
                _semaphore.Release();
                return;
            }

            var processorStrategy = jobsProcessorStrategies.FirstOrDefault(s => s.CanProcess(jobQueue.Type));

            if (processorStrategy == null)
            {
                _semaphore.Release();
                return;
            }

            _ = Task.Run(async () =>
            {
                using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

                try
                {
                    var processTask = processorStrategy.ProcessJobAsync(jobQueue, cancellation.Token);

                    async Task MonitorCancellationAsync()
                    {
                        try
                        {
                            while (!cancellation.Token.IsCancellationRequested)
                            {
                                var cancellationRequest = await cacheService.GetValueAsync<bool>($"cancelledJob_{jobQueue.Id}");

                                if (cancellationRequest)
                                {
                                    cancellation.Cancel();
                                    return;
                                }

                                await Task.Delay(TimeSpan.FromSeconds(2), cancellation.Token);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // Expected cancellation
                        }
                    }

                    var monitorTask = MonitorCancellationAsync();

                    await Task.WhenAny(processTask, monitorTask);

                    cancellation.Cancel();

                    await Task.WhenAll(processTask, monitorTask);
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Background job {JobId} was cancelled.", jobQueue.Id);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing background job {JobId}.", jobQueue.Id);
                }
                finally
                {
                    _processingJobIds.Remove(jobQueue.Id);
                    _semaphore.Release();
                }
            }, stoppingToken);

            _processingJobIds.Add(jobQueue.Id);
            await Task.Delay(1000, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.Error, ex.InnerException?.Message ?? ex.Message);
        }
    }
}
