using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using AgriIDMS.Domain.Interfaces;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_config["Smtp:From"]));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        message.Body = new TextPart(MimeKit.Text.TextFormat.Html)
        {
            Text = htmlBody
        };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(
            _config["Smtp:Host"],
            int.Parse(_config["Smtp:Port"]!),
            SecureSocketOptions.StartTls
        );

        await smtp.AuthenticateAsync(
            _config["Smtp:Username"],
            _config["Smtp:Password"]
        );

        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }
}
