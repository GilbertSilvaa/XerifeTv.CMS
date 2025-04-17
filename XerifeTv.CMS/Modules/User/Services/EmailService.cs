using System.Net;
using System.Net.Mail;
using XerifeTv.CMS.Modules.User.Interfaces;

namespace XerifeTv.CMS.Modules.User.Services;

public class EmailService : IEmailService
{
	private readonly SmtpClient _smtpClient;
	private readonly string _fromEmail;
	private readonly IConfiguration _configuration;

	public EmailService(IConfiguration configuration)
	{
		_configuration = configuration;
		_fromEmail = _configuration["EmailSettings:From"];
		_smtpClient = new SmtpClient("smtp.gmail.com")
		{
			Port = 587,
			Credentials = new NetworkCredential(_fromEmail, _configuration["EmailSettings:Password"]),
			EnableSsl = true
		};
	}
	
	public async Task SendEmailResetPasswordAsync(string toEmail, string resetCode)
	{
		var resetLink = _configuration["baseUrl"] + $"/Users/ResetPassword?code={resetCode.ToString()}";
		
		var mailMessage = new MailMessage()
		{
			From = new MailAddress(_fromEmail),
			Subject = "Redefinir Senha",
			Body = $@"<h4>Clique no link para redefinir sua senha: {resetLink}</h4><hr/>
			<span>O link possui um tempo de expiracao de 10 minutos</span>",
			IsBodyHtml = true
		};
		
		mailMessage.To.Add(toEmail);
		await _smtpClient.SendMailAsync(mailMessage);
	}
}