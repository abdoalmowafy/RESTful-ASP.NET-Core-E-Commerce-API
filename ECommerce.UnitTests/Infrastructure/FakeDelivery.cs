using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Entities.Enums;
using ECommerce.Infrastructure.Services;

namespace ECommerce.UnitTests.Infrastructure;

public sealed class FakeDelivery : INotificationDelivery
{
    public List<(string To, string Subject, string Body)> Emails { get; } = [];
    public List<(string To, string Body)> Sms { get; } = [];

    public Task SendEmailAsync(string to, string subject, string body)
    {
        Emails.Add((to, subject, body));
        return Task.CompletedTask;
    }

    public Task SendSmsAsync(string to, string body)
    {
        Sms.Add((to, body));
        return Task.CompletedTask;
    }

    public string? LastEmailCode()
        => System.Text.RegularExpressions.Regex.Match(
            Emails.LastOrDefault().Body ?? string.Empty, @"\b(\d{6})\b").Groups[1].Value is { Length: 6 } c ? c : null;
}
