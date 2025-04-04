using XerifeTv.CMS.Modules.User;
using XerifeTv.CMS.Modules.User.Enums;

namespace XerifeTv.CMS.Modules.User.Dtos.Response;

public class GetUserRequestDto
{
  public string Id { get; private set; } = string.Empty;
  public string UserName { get; private set; } = string.Empty;
  public EUserRole Role { get; private set; } = EUserRole.COMMON;
  public string RoleName => GetRoleName(Role);

  public static GetUserRequestDto FromEntity(UserEntity entity)
  {
    return new GetUserRequestDto
    {
      Id = entity.Id,
      UserName = entity.UserName,
      Role = entity.Role
    };
  }

  private static string GetRoleName(EUserRole role)
    => role switch
    {
      EUserRole.ADMIN => "Administrador",
      EUserRole.COMMON => "Usuário Comum",
      _ => "Visitante"
    };
}
