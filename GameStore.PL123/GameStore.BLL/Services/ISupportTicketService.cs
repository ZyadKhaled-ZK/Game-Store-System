namespace GameStore.BLL.Services;

public interface ISupportTicketService
{
    Task<SupportTicket> CreateAsync(string? userId, string? email, string subject, string message);
    Task<SupportTicket?> GetByIdAsync(string id);
    Task<List<SupportTicket>> GetUserTicketsAsync(string userId, int page = 1, int pageSize = 20);
    Task<int> GetUserTicketCountAsync(string userId);
    Task<List<SupportTicket>> GetAllAsync(int page = 1, int pageSize = 20);
    Task<int> GetCountAsync();
    Task<SupportTicketReply> AddReplyAsync(string ticketId, string? userId, string message);
    Task UpdateStatusAsync(string ticketId, TicketStatus status);
    Task<bool> IsOwnerAsync(string ticketId, string userId);
}
