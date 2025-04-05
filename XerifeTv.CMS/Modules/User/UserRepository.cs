using Microsoft.Extensions.Options;
using MongoDB.Driver;
using XerifeTv.CMS.Modules.Abstractions.Repositories;
using XerifeTv.CMS.Modules.User.Interfaces;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.User;

public sealed class UserRepository(IOptions<DBSettings> options)
  : BaseRepository<UserEntity>(ECollection.USERS, options), IUserRepository
{
  public async Task<UserEntity?> GetByUserNameAsync(string userName)
    => await _collection
        .Find(r => r.UserName.Equals(userName))
        .FirstOrDefaultAsync();

  public async Task<UserEntity?> GetByEmailAsync(string email)
		=> await _collection
				.Find(r => r.Email.Equals(email))
				.FirstOrDefaultAsync();
}
