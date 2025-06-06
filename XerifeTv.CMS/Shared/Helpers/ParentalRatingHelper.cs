namespace XerifeTv.CMS.Shared.Helpers;

public class ParentalRatingHelper
{
    public static int[] ParentalRatingList => [0, 10, 12, 14, 16, 18];

    public static Dictionary<int, string> ParentalRatingColors => new Dictionary<int, string>
    {
        { 0, "#0d915b" },
        { 10, "#0d6591" },
        { 12, "#94971a" },
        { 14, "#cf6a10" },
        { 16, "#ab1616" },
        { 18, "#232323" }
    };
}