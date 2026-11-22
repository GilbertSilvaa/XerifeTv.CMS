namespace XerifeTv.CMS.Modules.Common;

public record ItemsByCategory<T>(string Category, IEnumerable<T> Items);