using XerifeTv.CMS.Modules.Abstractions.Entities;
using XerifeTv.CMS.Modules.User.Enums;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Modules.User;

public class UserEntity : BaseEntity
{
    public string UserName { get; set; } = string.Empty;

    private string _email = string.Empty;
    public string Email
    {
        get => _email;
        set
        {
            if (!RegexHelper.IsValidEmail(value))
                throw new ArgumentException("Email invalido");

            _email = value;
        }
    }

    public string Password { get; set; } = string.Empty;
    public EUserRole Role { get; set; } = EUserRole.COMMON;
    public Guid? ResetPasswordGuid { get; set; }
    public DateTimeOffset? ResetPasswordGuidExpires { get; set; }
}