using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace ECommerce.Infrastructure.Services;

public class FcmSettings
{
    public const string SectionName = "Fcm";
    public string ServiceAccountJson { get; set; } = string.Empty;
}

public interface IPushSender
{
    bool IsConfigured { get; }
    Task<IReadOnlyList<string>> SendToTokensAsync(
        IReadOnlyList<string> tokens,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null);
}

/// <summary>
/// FCM HTTP v1 sender via Firebase Admin SDK.
/// Returns the subset of tokens that are dead (UNREGISTERED / INVALID_ARGUMENT)
/// so the registry can delete them — the token-invalidation loop.
/// Falls back to logging when no service account is configured (development).
/// </summary>
public class FirebasePushSender(IOptions<FcmSettings> options, ILogger<FirebasePushSender> logger) : IPushSender
{
    private static readonly object InitLock = new();
    private static volatile bool _initialized;

    private readonly FcmSettings _settings = options.Value;
    private readonly ILogger<FirebasePushSender> _logger = logger;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.ServiceAccountJson);

    public async Task<IReadOnlyList<string>> SendToTokensAsync(
        IReadOnlyList<string> tokens,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null)
    {
        if (tokens.Count == 0)
            return [];

        if (!IsConfigured)
        {
            foreach (var t in tokens.Take(3))
                _logger.LogWarning("DEV PUSH -> token: {TokenPrefix}… | {Title}: {Body}", t[..Math.Min(12, t.Length)], title, body);
            return [];
        }

        EnsureInitialized();

        var message = new MulticastMessage
        {
            Tokens = [.. tokens],
            Notification = new Notification { Title = title, Body = body },
            Data = data?.ToDictionary(kv => kv.Key, kv => kv.Value)
        };

        var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);

        var dead = new List<string>();
        for (var i = 0; i < response.Responses.Count; i++)
        {
            var r = response.Responses[i];
            if (r.IsSuccess) continue;

            var code = r.Exception?.MessagingErrorCode;
            if (code is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument)
                dead.Add(tokens[i]);
            else
                _logger.LogError(r.Exception, "FCM send failed for token index {Index}", i);
        }

        return dead;
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;

        lock (InitLock)
        {
            if (_initialized) return;

            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromJson(_settings.ServiceAccountJson)
            });

            _initialized = true;
        }
    }
}
