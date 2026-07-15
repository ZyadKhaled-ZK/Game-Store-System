using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace GameStore.PL.Services
{
    public class EmailService : IEmailService
    {
        private readonly SendGridSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<SendGridSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<bool> SendAsync(string toEmail, string toName, string subject, string htmlBody, string? textBody = null)
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey) ||
                string.IsNullOrWhiteSpace(_settings.FromEmail))
            {
                _logger.LogWarning("SendGrid not configured — skipping email to {Email}", toEmail);
                return false;
            }

            try
            {
                var client = new SendGridClient(_settings.ApiKey);
                var from = new EmailAddress(_settings.FromEmail, _settings.FromName);
                var to = new EmailAddress(toEmail, toName);
                var msg = MailHelper.CreateSingleEmail(from, to, subject, textBody ?? "", htmlBody);
                var response = await client.SendEmailAsync(msg);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Body.ReadAsStringAsync();
                    _logger.LogError("SendGrid failed ({Status}): {Body}", response.StatusCode, body);
                    return false;
                }

                _logger.LogInformation("Email sent to {Email} — {Subject}", toEmail, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                return false;
            }
        }
    }
}
