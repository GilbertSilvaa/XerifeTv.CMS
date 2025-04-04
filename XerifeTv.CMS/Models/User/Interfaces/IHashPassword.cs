namespace XerifeTv.CMS.Models.User.Interfaces;

public interface IHashPassword
{
	string Encrypt(string password);
	bool Verify(string password, string hash);
}
