using XerifeTv.CMS.Modules.Abstractions.Interfaces;

namespace XerifeTv.CMS.Modules.User.Interfaces;

public interface IUserRepository : IBaseRepository<UserEntity>
{
    Task<UserEntity?> GetByUsernameAsync(string userName);
    Task<UserEntity?> GetByEmailAsync(string email);
    Task<UserEntity?> GetByResetPasswordGuidAsync(Guid guid);
}