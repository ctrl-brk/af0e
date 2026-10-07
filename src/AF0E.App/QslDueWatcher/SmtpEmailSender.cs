using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace QslDueWatcher;

public interface IEmailSender
{
    Task SendAsync(ReminderEmail email, CancellationToken cancellationToken);
}

public sealed class SmtpEmailSender(IOptions<AppSettings> options) : IEmailSender
{
    private readonly EmailSettings _settings = options.Value.Email;

    public async Task SendAsync(ReminderEmail email, CancellationToken cancellationToken)
    {
        using var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.From));
        message.To.AddRange(InternetAddressList.Parse(_settings.To));
        message.Subject = email.Subject;

        var body = new MultipartAlternative
        {
            new TextPart(TextFormat.Plain) { Text = email.PlainTextBody },
            new TextPart(TextFormat.Html) { Text = email.HtmlBody }
        };
        message.Body = body;

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Smtp.Server, _settings.Smtp.Port, SecureSocketOptions.Auto, cancellationToken);

        try
        {
            if (!string.IsNullOrWhiteSpace(_settings.Smtp.User))
                await client.AuthenticateAsync(_settings.Smtp.User, _settings.Smtp.Password ?? string.Empty, cancellationToken);

            await client.SendAsync(message, cancellationToken);
        }
        finally
        {
            if (client.IsConnected)
                await client.DisconnectAsync(true, cancellationToken);
        }
    }
}
