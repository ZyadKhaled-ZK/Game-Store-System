using GameStore.DAL.Entities;
using GameStore.DAL.Repo;
using GameStore.PL.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GameStore.PL.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IUnitOfWork _uow;

    public NotificationService(IHubContext<NotificationHub> hubContext, IUnitOfWork uow)
    {
        _hubContext = hubContext;
        _uow = uow;
    }

    public async Task SendToUserAsync(string userId, string title, string message, string type, string? category = null, string? referenceId = null, string? referenceUrl = null, string? senderUserId = null)
    {
        var notification = new UserNotification
        {
            UserId = userId,
            SenderUserId = senderUserId,
            Title = title,
            Message = message,
            Type = type,
            Category = category ?? "General",
            ReferenceId = referenceId,
            ReferenceUrl = referenceUrl
        };

        await _uow.Repository<UserNotification>().AddAsync(notification);
        await _uow.SaveChangesAsync();

        var senderName = string.IsNullOrEmpty(senderUserId) ? null
            : await _uow.Repository<User>().Query()
                .Where(u => u.Id == senderUserId)
                .Select(u => u.Username)
                .FirstOrDefaultAsync();

        await _hubContext.Clients.Group(userId).SendAsync("ReceiveNotification", new
        {
            id = notification.Id,
            title,
            message,
            type,
            senderName,
            category = notification.Category,
            referenceUrl,
            createdAt = notification.CreatedAt
        });
    }

    public async Task SendToAdminsAsync(string title, string message, string type, string? category = null, string? referenceId = null, string? referenceUrl = null)
    {
        var notification = new UserNotification
        {
            UserId = null,
            Title = title,
            Message = message,
            Type = type,
            Category = category ?? "General",
            ReferenceId = referenceId,
            ReferenceUrl = referenceUrl
        };

        await _uow.Repository<UserNotification>().AddAsync(notification);
        await _uow.SaveChangesAsync();

        await _hubContext.Clients.Group("Admins").SendAsync("ReceiveNotification", new
        {
            id = notification.Id,
            title,
            message,
            type,
            category = notification.Category,
            referenceUrl,
            createdAt = notification.CreatedAt
        });
    }
}
