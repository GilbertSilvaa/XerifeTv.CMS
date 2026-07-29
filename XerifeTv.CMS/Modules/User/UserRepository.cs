using Microsoft.Extensions.Options;
using MongoDB.Driver;
using XerifeTv.CMS.Modules.Abstractions.Repositories;
using XerifeTv.CMS.Modules.User.Interfaces;
using XerifeTv.CMS.Shared.Database.MongoDB;

namespace XerifeTv.CMS.Modules.User;

public sealed class UserRepository(IOptions<DBSettings> options, IMongoClient mongoClient)
  : BaseRepository<UserEntity>(ECollection.USERS, options, mongoClient), IUserRepository
{
    public async Task<UserEntity?> GetByUsernameAsync(string userName)
      => await _collection
          .Find(r => r.UserName.Equals(userName))
          .FirstOrDefaultAsync();

    public async Task<UserEntity?> GetByEmailAsync(string email)
          => await _collection
                  .Find(r => r.Email.Equals(email))
                  .FirstOrDefaultAsync();

    public async Task<UserEntity?> GetByResetPasswordGuidAsync(Guid guid)
          => await _collection
                  .Find(r => r.ResetPasswordGuid.Equals(guid))
                  .FirstOrDefaultAsync();
}
