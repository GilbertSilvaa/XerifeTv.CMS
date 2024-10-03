namespace XerifeTv.CMS.Models.Abstractions.Services;

public interface ICacheService
{
  T? GetValue<T>(string key);
  void SetValue<T>(string key, T value);
  void Remove(string key);
}