namespace GameStore.PL.Services;

public interface INotificationService
{
    Task SendToUserAsync(string userId, string title, string message, string type);
    Task SendToAdminsAsync(string title, string message, string type);
}
