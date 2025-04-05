using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.User.Enums;

namespace XerifeTv.CMS.Modules.User;

public class UserEntity : BaseEntity
{
  public string UserName { get; set; } = string.Empty;
  public string Email { get; set; } = string.Empty;
  public string Password { get; set; } = string.Empty;
  public EUserRole Role { get; set; } = EUserRole.COMMON;
}
