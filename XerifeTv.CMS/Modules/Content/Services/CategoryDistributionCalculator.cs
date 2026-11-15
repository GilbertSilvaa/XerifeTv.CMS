using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.Common;

namespace XerifeTv.CMS.Modules.Content.Services;

public sealed record CategoryDistributionInput(
    string Category,
    IReadOnlyCollection<string> ItemIds);

public sealed record CategoryDistributionResult(
    IReadOnlyList<string> Ordered,
    IReadOnlyList<string> Redundant,
    IReadOnlyList<string> Empty);

public sealed class CategoryDistributionCalculator
{
    private const double RedundancyThreshold = 0.7;

    public CategoryDistributionResult Calculate<T>(IEnumerable<ItemsByCategory<T>> categories)
        where T : BaseEntity
    {
        return Calculate(categories.Select(c => new CategoryDistributionInput(
            c.Category, [.. c.Items.Select(i => i.Id)])));
    }

    public CategoryDistributionResult Calculate(IEnumerable<CategoryDistributionInput> categories)
    {
        var categoryList = categories.ToList();

        if (categoryList.Count <= 2)
            return new(
                [.. categoryList.Select(c => c.Category)],
                [],
                [.. categoryList.Where(IsEmpty).Select(c => c.Category)]);

        int rawCount = categoryList.Count;

        var rawItemSets = categoryList
            .Select(x => x.ItemIds.ToHashSet())
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

        var redundant = categoryList
            .Where((_, idx) => excluded.Contains(idx))
            .Select(c => c.Category)
            .ToList();

        var filteredList = categoryList
            .Where((_, idx) => !excluded.Contains(idx))
            .ToList();

        if (filteredList.Count <= 2)
        {
            return new(
                [.. filteredList.Select(c => c.Category)],
                redundant,
                [.. filteredList.Where(IsEmpty).Select(c => c.Category)]);
        }

        int count = filteredList.Count;

        var itemSets = filteredList
            .Select(x => x.ItemIds.ToHashSet())
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

        var ordered = result.Select(i => filteredList[i]).ToList();

        return new(
            [.. ordered.Select(c => c.Category)],
            redundant,
            [.. ordered.Where(IsEmpty).Select(c => c.Category)]);
    }

    private static bool IsEmpty(CategoryDistributionInput category)
        => category.ItemIds.Count == 0;
}
