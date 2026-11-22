using System.Net;
using System.Net.Mail;
using XerifeTv.CMS.Modules.User.Interfaces;

namespace XerifeTv.CMS.Modules.User.Services;

public class EmailService(IConfiguration configuration) : IEmailService
{
    private readonly string? _fromEmail = configuration["EmailSettings:From"];
    private readonly SmtpClient _smtpClient = new("smtp.gmail.com")
    {
        Port = 587,
        Credentials = new NetworkCredential(configuration["EmailSettings:From"], configuration["EmailSettings:Password"]),
        EnableSsl = true
    };

    public async Task SendEmailResetPasswordAsync(string toEmail, string resetCode)
    {
        var resetLink = configuration["baseUrl"] + $"/Users/ResetPassword?code={resetCode.ToString()}";

        var mailMessage = new MailMessage()
        {
            From = new MailAddress(_fromEmail!),
            Subject = "Redefinir Senha",
            Body = $@"<h4>Clique no link para redefinir sua senha: {resetLink}</h4><hr/>
			<span>O link possui um tempo de expiracao de 10 minutos</span>",
            IsBodyHtml = true
        };

        mailMessage.To.Add(toEmail);
        await _smtpClient.SendMailAsync(mailMessage);
    }
}
