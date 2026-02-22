using ISS.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;

namespace ISS.Infrastructure.Notifications;

public sealed class TwilioSmsSender(
    IHttpClientFactory httpClientFactory,
    IOptions<TwilioSmsOptions> options,
    ILogger<TwilioSmsSender> logger) : ISmsSender
{
    public async Task SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.AccountSid) || string.IsNullOrWhiteSpace(o.AuthToken) || string.IsNullOrWhiteSpace(o.From))
        {
            throw new InvalidOperationException("Twilio SMS is not configured.");
        }

        var client = httpClientFactory.CreateClient();
        var url = new Uri($"https://api.twilio.com/2010-04-01/Accounts/{o.AccountSid}/Messages.json");

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{o.AccountSid}:{o.AuthToken}"));
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
        req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"] = message.To,
            ["From"] = o.From,
            ["Body"] = message.Body
        });

        using var resp = await client.SendAsync(req, cancellationToken);
        if (!resp.IsSuccessStatusCode)
        {
            var detail = await resp.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Twilio SMS send failed ({Status}): {Detail}", resp.StatusCode, detail);
            resp.EnsureSuccessStatusCode();
        }
    }
}

