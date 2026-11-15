using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Response;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Content;
using XerifeTv.CMS.Modules.Content.Interfaces;
using XerifeTv.CMS.Modules.Content.Services;
using XerifeTv.CMS.Modules.Movie.Interfaces;
using XerifeTv.CMS.Modules.Series.Interfaces;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue.ProcessorStrategies;

public class CalculateCategoryDistributionBackgroundJobProcessorStrategy(
    IServiceProvider serviceProvider,
    CategoryDistributionCalculator categoryDistributionCalculator) : IBackgroundJobProcessorStrategy
{
    public async Task ProcessJobAsync(GetBackgroundJobResponseDto job, CancellationToken cancellation)
    {
        int pageSizeContent = ContentConstants.DefaultPageSizeContent;
        int pageSizeMin = ContentConstants.DefaultPageSizeMin;

        using var scope = serviceProvider.CreateScope();
        var backgroundJobQueueService = scope.ServiceProvider.GetRequiredService<IBackgroundJobQueueService>();
        var contentSettingsRepository = scope.ServiceProvider.GetRequiredService<IContentSettingsRepository>();

        var contentSettings = await contentSettingsRepository.GetContentSettingsAsync();

        if (job.Type == EBackgroundJobType.CALCULATE_CATEGORY_DISTRIBUTION_FOR_CONTENT_API_MOVIES)
        {
            var movieRepository = scope.ServiceProvider.GetRequiredService<IMovieRepository>();
            var categories = await movieRepository.GetCategoriesWithCountAsync();
            var categoriesWithEnoughContent = categories.Where(c => c.Count >= pageSizeMin).ToList();

            var updateBackgroundJobDto = new UpdateBackgroundJobRequestDto
            {
                Id = job.Id,
                TotalRecordsToProcess = categoriesWithEnoughContent.Count,
                TotalSuccessfulRecords = 0,
                TotalProcessedRecords = 0,
                Status = EBackgroundJobStatus.PROCESSING
            };

            await backgroundJobQueueService.UpdateAsync(updateBackgroundJobDto);

            var moviesByCategories = await movieRepository.GetGroupByCategoryAsync(
                new(
                    [.. categoriesWithEnoughContent.Select(c => c.Category)],
                    CurrentPage: 1,
                    LimitResults: pageSizeContent
                ));

            var spreadCategories = categoryDistributionCalculator
                .Calculate(moviesByCategories)
                .Ordered;

            if (contentSettings == null)
            {
                contentSettings = new ContentSettingsEntity
                {
                    MovieCategoriesDistribution = [.. spreadCategories]
                };
            }
            else
            {
                contentSettings.MovieCategoriesDistribution = [.. spreadCategories];
            }

            await contentSettingsRepository.CreateOrUpdateAsync(contentSettings);

            updateBackgroundJobDto = new UpdateBackgroundJobRequestDto
            {
                Id = job.Id,
                TotalRecordsToProcess = categoriesWithEnoughContent.Count,
                TotalSuccessfulRecords = categoriesWithEnoughContent.Count,
                TotalProcessedRecords = categoriesWithEnoughContent.Count,
                Status = EBackgroundJobStatus.COMPLETED
            };

            await backgroundJobQueueService.UpdateAsync(updateBackgroundJobDto);
        }

        if (job.Type == EBackgroundJobType.CALCULATE_CATEGORY_DISTRIBUTION_FOR_CONTENT_API_SERIES)
        {
            var seriesRepository = scope.ServiceProvider.GetRequiredService<ISeriesRepository>();
            var categories = await seriesRepository.GetCategoriesWithCountAsync();
            var categoriesWithEnoughContent = categories.Where(c => c.Count >= pageSizeMin).ToList();

            var updateBackgroundJobDto = new UpdateBackgroundJobRequestDto
            {
                Id = job.Id,
                TotalRecordsToProcess = categoriesWithEnoughContent.Count,
                TotalSuccessfulRecords = 0,
                TotalProcessedRecords = 0,
                Status = EBackgroundJobStatus.PROCESSING
            };

            await backgroundJobQueueService.UpdateAsync(updateBackgroundJobDto);

            var seriesByCategories = await seriesRepository.GetGroupByCategoryAsync(
                new(
                    [.. categoriesWithEnoughContent.Select(c => c.Category)],
                    CurrentPage: 1,
                    LimitResults: pageSizeContent
                ));

            var spreadCategories = categoryDistributionCalculator
                .Calculate(seriesByCategories)
                .Ordered;

            if (contentSettings == null)
            {
                contentSettings = new ContentSettingsEntity
                {
                    SeriesCategoriesDistribution = [.. spreadCategories]
                };
            }
            else
            {
                contentSettings.SeriesCategoriesDistribution = [.. spreadCategories];
            }

            await contentSettingsRepository.CreateOrUpdateAsync(contentSettings);

            updateBackgroundJobDto = new UpdateBackgroundJobRequestDto
            {
                Id = job.Id,
                TotalRecordsToProcess = categoriesWithEnoughContent.Count,
                TotalSuccessfulRecords = categoriesWithEnoughContent.Count,
                TotalProcessedRecords = categoriesWithEnoughContent.Count,
                Status = EBackgroundJobStatus.COMPLETED
            };

            await backgroundJobQueueService.UpdateAsync(updateBackgroundJobDto);
        }
    }

    public bool CanProcess(EBackgroundJobType jobType)
        => jobType is
        EBackgroundJobType.CALCULATE_CATEGORY_DISTRIBUTION_FOR_CONTENT_API_MOVIES or
        EBackgroundJobType.CALCULATE_CATEGORY_DISTRIBUTION_FOR_CONTENT_API_SERIES;
}
