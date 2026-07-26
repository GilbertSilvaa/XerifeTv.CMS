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
        int pageSizeContent = ContentConstants.DefaultPageSizeContent;
        int pageSizeMin = ContentConstants.DefaultPageSizeMin;

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

    private static IEnumerable<string> SpreadCategories<T>(IEnumerable<ItemsByCategory<T>> categories) where T : BaseEntity
    {
        const double RedundancyThreshold = 0.7;

        var categoryList = categories.ToList();

        if (categoryList.Count <= 2)
            return categoryList.Select(c => c.Category);

        int rawCount = categoryList.Count;

        var rawItemSets = categoryList
            .Select(x => x.Items.Select(i => i.Id).ToHashSet())
            .ToArray();

        var excluded = new HashSet<int>();

        for (int i = 0; i < rawCount; i++)
        {
            if (excluded.Contains(i))
                continue;

            for (int j = 0; j < rawCount; j++)
            {
                if (i == j || excluded.Contains(j))
                    continue;

                if (rawItemSets[j].Count < rawItemSets[i].Count)
                    continue;

                if (rawItemSets[i].Count == 0)
                    continue;

                int common = rawItemSets[i].Count(item => rawItemSets[j].Contains(item));
                double overlapRatio = (double)common / rawItemSets[i].Count;

                if (overlapRatio >= RedundancyThreshold)
                {
                    excluded.Add(i);
                    break;
                }
            }
        }

        var filteredList = categoryList
            .Where((_, idx) => !excluded.Contains(idx))
            .ToList();

        if (filteredList.Count <= 2)
            return filteredList.Select(c => c.Category);

        int count = filteredList.Count;

        var itemSets = filteredList
            .Select(x => x.Items.Select(i => i.Id).ToHashSet())
            .ToArray();

        var similarity = new int[count, count];

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

                similarity[i, j] = common;
                similarity[j, i] = common;
            }
        });

        var categorySize = itemSets.Select(s => s.Count).ToArray();
        int maxSize = categorySize.Max();

        var result = new List<int>(count);
        var remaining = new HashSet<int>(Enumerable.Range(0, count));

        int startIndex = Array.IndexOf(categorySize, maxSize);

        result.Add(startIndex);
        remaining.Remove(startIndex);

        while (remaining.Count > 0)
        {
            int bestCandidate = -1;
            double bestScore = double.MaxValue;

            foreach (var candidate in remaining)
            {
                int similarityScore = 0;

                foreach (var selected in result)
                {
                    similarityScore += similarity[candidate, selected];
                }

                double sizeBonus = (double)categorySize[candidate] / maxSize;
                double score = similarityScore - sizeBonus;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestCandidate = candidate;
                }
            }

            result.Add(bestCandidate);
            remaining.Remove(bestCandidate);
        }

        return result.Select(i => filteredList[i].Category);
    }
}