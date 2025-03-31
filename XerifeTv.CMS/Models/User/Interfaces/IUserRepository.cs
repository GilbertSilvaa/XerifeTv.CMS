using XerifeTv.CMS.Models.Abstractions.Interfaces;

namespace XerifeTv.CMS.Models.User.Interfaces;

public interface IUserRepository : IBaseRepository<UserEntity>
{
  Task<UserEntity?> GetByUserNameAsync(string userName);
}