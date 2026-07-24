using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Response;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Content;
using XerifeTv.CMS.Modules.Content.Interfaces;
using XerifeTv.CMS.Modules.Movie.Interfaces;
using XerifeTv.CMS.Modules.Series.Interfaces;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue.ProcessorStrategies;

public class CalculateCategoryDistributionBackgroundJobProcessorStrategy : IBackgroundJobProcessorStrategy
{
    private readonly IServiceProvider _serviceProvider;

    public CalculateCategoryDistributionBackgroundJobProcessorStrategy(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task ProcessJobAsync(GetBackgroundJobResponseDto job)
    {
        const int pageSizeContent = 16;
        const int pageSizeMin = 10;

        using var scope = _serviceProvider.CreateScope();
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

            var spreadCategories = SpreadCategories(moviesByCategories);

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

            updateBackgroundJobDto.Status = EBackgroundJobStatus.COMPLETED;
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

            var spreadCategories = SpreadCategories(seriesByCategories);

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

            updateBackgroundJobDto.Status = EBackgroundJobStatus.COMPLETED;
            await backgroundJobQueueService.UpdateAsync(updateBackgroundJobDto);
        }
    }

    public bool CanProcess(EBackgroundJobType jobType)
        => jobType is
        EBackgroundJobType.CALCULATE_CATEGORY_DISTRIBUTION_FOR_CONTENT_API_MOVIES or
        EBackgroundJobType.CALCULATE_CATEGORY_DISTRIBUTION_FOR_CONTENT_API_SERIES;

    private static IEnumerable<string> SpreadCategories<T>(IEnumerable<ItemsByCategory<T>> categories) where T : BaseEntity
    {
        var categoryList = categories.ToList();

        if (categoryList.Count <= 2)
            return categoryList.Select(c => c.Category);

        int count = categoryList.Count;

        var itemSets = categoryList
            .Select(x => x.Items.Select(i => i.Id).ToHashSet())
            .ToArray();

        var similarity = new double[count, count];

        Parallel.For(0, count, i =>
        {
            for (int j = i + 1; j < count; j++)
            {
                var setA = itemSets[i];
                var setB = itemSets[j];

                var smaller = setA.Count < setB.Count ? setA : setB;
                var larger = setA.Count < setB.Count ? setB : setA;

                int common = 0;
                foreach (var item in smaller)
                {
                    if (larger.Contains(item))
                        common++;
                }

                int union = setA.Count + setB.Count - common;
                double jaccard = union == 0 ? 0.0 : (double)common / union;

                similarity[i, j] = jaccard;
                similarity[j, i] = jaccard;
            }
        });

        int startA = 0, startB = 1;
        double lowestPairSimilarity = double.MaxValue;

        for (int i = 0; i < count; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                if (similarity[i, j] < lowestPairSimilarity)
                {
                    lowestPairSimilarity = similarity[i, j];
                    startA = i;
                    startB = j;
                }
            }
        }

        var result = new List<int>(count) { startA };
        var remaining = new HashSet<int>(Enumerable.Range(0, count));
        remaining.Remove(startA);

        var score = new double[count];
        foreach (var c in remaining)
            score[c] = similarity[startA, c];

        while (remaining.Count > 0)
        {
            int bestCandidate = -1;
            double bestScore = double.MaxValue;
            int bestOriginalIndex = int.MaxValue;

            foreach (var candidate in remaining)
            {
                double s = score[candidate];

                if (s < bestScore || (s == bestScore && candidate < bestOriginalIndex))
                {
                    bestScore = s;
                    bestCandidate = candidate;
                    bestOriginalIndex = candidate;
                }
            }

            result.Add(bestCandidate);
            remaining.Remove(bestCandidate);

            int distanceFromEnd = result.Count;
            foreach (var c in remaining)
                score[c] += similarity[bestCandidate, c] / distanceFromEnd;
        }

        return result.Select(i => categoryList[i].Category);
    }
}