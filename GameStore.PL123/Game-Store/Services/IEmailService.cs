namespace GameStore.PL.Services
{
    public interface IEmailService
    {
        Task<bool> SendAsync(string toEmail, string toName, string subject, string htmlBody, string? textBody = null);
    }
}
