namespace XerifeTv.CMS.Models.Abstractions;

public record ItemsByCategory<T>(string Category, IEnumerable<T> Items);