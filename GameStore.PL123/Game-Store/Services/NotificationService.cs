using GameStore.PL.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace GameStore.PL.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendToUserAsync(string userId, string title, string message, string type)
    {
        await _hubContext.Clients.Group(userId).SendAsync("ReceiveNotification", new
        {
            title,
            message,
            type
        });
    }

    public async Task SendToAdminsAsync(string title, string message, string type)
    {
        await _hubContext.Clients.Group("Admins").SendAsync("ReceiveNotification", new
        {
            title,
            message,
            type
        });
    }
}
