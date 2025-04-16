namespace XerifeTv.CMS.Modules.User.Interfaces;

public interface IEmailService
{
  Task SendPasswordResetEmailAsync(string email, string resetLink);
}