namespace XerifeTv.CMS.Modules.User.Interfaces;

public interface IEmailService
{
  Task SendEmailResetPasswordAsync(string toEmail, string resetCode);
}