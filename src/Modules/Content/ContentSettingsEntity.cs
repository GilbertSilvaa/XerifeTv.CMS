using XerifeTv.CMS.Modules.Abstractions.Entities;

namespace XerifeTv.CMS.Modules.Content;

public class ContentSettingsEntity : BaseEntity
{
    public List<string> MovieCategoriesDistribution = [];
    public List<string> SeriesCategoriesDistribution = [];
}