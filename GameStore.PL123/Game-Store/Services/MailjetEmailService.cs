using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace GameStore.PL.Services
{
    public class MailjetEmailService : IEmailService
    {
        private readonly MailjetApiSettings _settings;
        private readonly ILogger<MailjetEmailService> _logger;
        private static readonly HttpClient _httpClient = new();

        public MailjetEmailService(IOptions<MailjetApiSettings> settings, ILogger<MailjetEmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<bool> SendAsync(string toEmail, string toName, string subject, string htmlBody, string? textBody = null)
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey) ||
                string.IsNullOrWhiteSpace(_settings.SecretKey) ||
                string.IsNullOrWhiteSpace(_settings.FromEmail))
            {
                _logger.LogWarning("Mailjet not configured — skipping email to {Email}", toEmail);
                return false;
            }

            try
            {
                var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_settings.ApiKey}:{_settings.SecretKey}"));

                var payload = new
                {
                    Messages = new[]
                    {
                        new
                        {
                            From = new { Email = _settings.FromEmail, Name = _settings.FromName },
                            To = new[] { new { Email = toEmail, Name = toName } },
                            Subject = subject,
                            TextPart = textBody ?? "",
                            HTMLPart = htmlBody
                        }
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.mailjet.com/v3.1/send")
                {
                    Content = content,
                    Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth) }
                };

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Mailjet failed ({Status}): {Body}", response.StatusCode, responseBody);
                    return false;
                }

                _logger.LogInformation("Mailjet email sent to {Email} — {Subject}", toEmail, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mailjet failed to send email to {Email}", toEmail);
                return false;
            }
        }
    }
}
