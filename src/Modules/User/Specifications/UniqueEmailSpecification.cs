using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.User.Interfaces;

namespace XerifeTv.CMS.Modules.User.Specifications;

public sealed class UniqueEmailSpecification(IUserRepository repository) : ISpecification<UserEntity>
{
    public async Task<bool> IsSatisfiedByAsync(UserEntity user)
    {
        try
        {
            var userByEmail = await repository.GetByEmailAsync(user.Email);
            return userByEmail == null || userByEmail.Id == user.Id;
        }
        catch
        {
            return false;
        }
    }
}
