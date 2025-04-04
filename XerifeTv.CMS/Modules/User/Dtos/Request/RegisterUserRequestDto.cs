using XerifeTv.CMS.Modules.User;
using XerifeTv.CMS.Modules.User.Enums;

namespace XerifeTv.CMS.Modules.User.Dtos.Request;

public class RegisterUserRequestDto
{
  public string UserName { get; init; } = string.Empty;
  public string Password { get; init; } = string.Empty;
  public EUserRole Role { get; init; } = EUserRole.COMMON;

  public UserEntity ToEntity()
  {
    return new UserEntity
    {
      UserName = UserName,
      Password = Password,
      Role = Role
    };
  }
}