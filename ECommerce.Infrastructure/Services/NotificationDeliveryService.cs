using System.Text;
using MailKit.Net.Smtp;
using MimeKit;

namespace ECommerce.Infrastructure.Services;

public interface INotificationDelivery
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendSmsAsync(string to, string body);
}

public class SmtpSettings
{
    public const string SectionName = "Smtp";
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromName { get; set; } = "StoreFront";
    public string FromAddress { get; set; } = "no-reply@store.com";
}

/// <summary>
/// Sends through SMTP when configured; otherwise logs the message (development mode).
/// SMS delivery is a stub — plug an SMS provider here.
/// </summary>
public class NotificationDeliveryService(
    IOptions<SmtpSettings> smtpOptions,
    ILogger<NotificationDeliveryService> logger) : INotificationDelivery
{
    private readonly SmtpSettings _smtp = smtpOptions.Value;
    private readonly ILogger<NotificationDeliveryService> _logger = logger;

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(_smtp.Host))
        {
            _logger.LogWarning(
                "DEV EMAIL (SMTP not configured) -> To: {To} | Subject: {Subject} | Body: {Body}",
                to, subject, body);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtp.Host, _smtp.Port, MailKit.Security.SecureSocketOptions.StartTls);
        if (!string.IsNullOrEmpty(_smtp.Username))
            await client.AuthenticateAsync(_smtp.Username, _smtp.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    public Task SendSmsAsync(string to, string body)
    {
        _logger.LogWarning("DEV SMS (provider not configured) -> To: {To} | Body: {Body}", to, body);
        return Task.CompletedTask;
    }
}
