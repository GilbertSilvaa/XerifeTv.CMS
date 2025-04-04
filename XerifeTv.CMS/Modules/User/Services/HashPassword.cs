using System.Security.Cryptography;
using System.Text;
using XerifeTv.CMS.Modules.User.Interfaces;

namespace XerifeTv.CMS.Modules.User.Services;

public sealed class HashPassword(IConfiguration _configuration) : IHashPassword
{
  private const int keySize = 55;
  private const int interations = 450000;
  private readonly HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA256;
  private readonly byte[] salt = Encoding.UTF8.GetBytes(
    _configuration["Hash:Salt"] ?? string.Empty);

  public string Encrypt(string password)
  {
    var hash = Rfc2898DeriveBytes.Pbkdf2(
      Encoding.UTF8.GetBytes(password), 
      salt, 
      interations, 
      hashAlgorithm, 
      keySize);

    return Convert.ToHexString(hash);
  }

  public bool Verify(string password, string hash)
  {
    var hashToCompare = Rfc2898DeriveBytes.Pbkdf2(
      password, 
      salt, 
      interations, 
      hashAlgorithm, 
      keySize);

    return CryptographicOperations.FixedTimeEquals(
      hashToCompare, 
      Convert.FromHexString(hash));
  }
}