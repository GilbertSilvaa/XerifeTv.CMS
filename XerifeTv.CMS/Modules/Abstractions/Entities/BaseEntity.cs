namespace XerifeTv.CMS.Modules.Abstractions.Entities;

public abstract class BaseEntity
{
  public string Id { get; set; } = Guid.NewGuid().ToString();
  public DateTime CreateAt { get; set; } = DateTime.UtcNow;
  public DateTime? UpdateAt { get; set; }
}
