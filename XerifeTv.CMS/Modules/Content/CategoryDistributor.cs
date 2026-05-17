using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.Common;

namespace XerifeTv.CMS.Modules.Content;

public static class CategoryDistributor
{
    public static IEnumerable<ItemsByCategory<T>> SpreadCategories<T>(IEnumerable<ItemsByCategory<T>> categories) where T : BaseEntity
    {
        var categoryList = categories.ToList();

        if (categoryList.Count <= 2)
            return categoryList;

        int count = categoryList.Count;

        var itemSets = categoryList
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

        var result = new List<int>(count);
        var remaining = new HashSet<int>(Enumerable.Range(0, count));

        result.Add(0);
        remaining.Remove(0);

        while (remaining.Count > 0)
        {
            int bestCandidate = -1;
            int bestScore = int.MaxValue;

            foreach (var candidate in remaining)
            {
                int score = 0;

                foreach (var selected in result)
                {
                    score += similarity[candidate, selected];
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    bestCandidate = candidate;
                }
            }

            result.Add(bestCandidate);
            remaining.Remove(bestCandidate);
        }

        return result.Select(i => categoryList[i]);
    }
}