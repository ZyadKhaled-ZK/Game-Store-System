namespace GameStore.PL.Services;

public interface INotificationService
{
    Task SendToUserAsync(string userId, string title, string message, string type, string? category = null, string? referenceId = null, string? referenceUrl = null, string? senderUserId = null);
    Task SendToAdminsAsync(string title, string message, string type, string? category = null, string? referenceId = null, string? referenceUrl = null);
}
