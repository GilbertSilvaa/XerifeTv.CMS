namespace XerifeTv.CMS.Modules.User.Interfaces;

public interface IHashPassword
{
    string Encrypt(string password);
    bool Verify(string password, string hash);
}
