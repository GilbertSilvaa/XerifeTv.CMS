using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.User.Interfaces;

namespace XerifeTv.CMS.Modules.User.Specifications;

public sealed class UniqueUsernameSpecification(IUserRepository _repository) : ISpecification<UserEntity>
{
    public async Task<bool> IsSatisfiedByAsync(UserEntity user)
    {
        try
        {
            var userByName = await _repository.GetByUsernameAsync(user.UserName);
            return userByName == null || userByName.Id == user.Id;
        }
        catch
        {
            return false;
        }
    }
}