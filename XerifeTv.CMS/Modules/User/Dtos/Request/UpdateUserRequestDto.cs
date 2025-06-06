using XerifeTv.CMS.Modules.User.Enums;

namespace XerifeTv.CMS.Modules.User.Dtos.Request;

public class UpdateUserRequestDto
{
    public string Id { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public EUserRole? Role { get; init; }

    public UserEntity ToEntity()
    {
        return new UserEntity
        {
            Id = Id,
            UserName = UserName,
            Email = Email,
            Role = Role ?? EUserRole.VISITOR
        };
    }
}