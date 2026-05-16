using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Movie;

namespace XerifeTv.CMS.Modules.Content;

public static class CategoryDistributor
{
    public static IEnumerable<ItemsByCategory<T>> SpreadCategories<T>(IEnumerable<ItemsByCategory<T>> categories) where T : BaseEntity
    {
        if (categories.Count() <= 1)
            return [.. categories];

        var categoryItemSets = categories.ToDictionary(
            c => c.Category,
            c => c.Items
                .Select(x => x.Id)
                .ToHashSet());

        var similarityMatrix = new Dictionary<string, Dictionary<string, int>>();

        foreach (var categoryA in categories)
        {
            similarityMatrix[categoryA.Category] = [];

            foreach (var categoryB in categories)
            {
                if (categoryA.Category == categoryB.Category)
                    continue;

                var setA = categoryItemSets[categoryA.Category];
                var setB = categoryItemSets[categoryB.Category];

                var smaller = setA.Count < setB.Count ? setA : setB;
                var larger = setA.Count < setB.Count ? setB : setA;

                int commonCount = 0;

                foreach (var item in smaller)
                {
                    if (larger.Contains(item))
                        commonCount++;
                }

                similarityMatrix[categoryA.Category][categoryB.Category] = commonCount;
            }
        }

        var remaining = categories.ToList();
        var result = new List<ItemsByCategory<T>>();

        var start = categories
            .OrderByDescending(c =>
                similarityMatrix[c.Category].Values.Sum())
            .First();

        result.Add(start);
        remaining.Remove(start);

        while (remaining.Count > 0)
        {
            ItemsByCategory<T>? bestCandidate = null;

            int bestScore = int.MaxValue;

            foreach (var candidate in remaining)
            {
                int score = 0;

                foreach (var selected in result)
                {
                    score += similarityMatrix[candidate.Category]
                        .GetValueOrDefault(selected.Category);
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    bestCandidate = candidate;
                }
            }

            result.Add(bestCandidate!);
            remaining.Remove(bestCandidate!);
        }

        return result;
    }
}