using Microsoft.EntityFrameworkCore;

namespace GameStore.BLL.Services;

public class SupportTicketService : ISupportTicketService
{
    private readonly IUnitOfWork _uow;

    public SupportTicketService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<SupportTicket> CreateAsync(string? userId, string? email, string subject, string message)
    {
        var ticket = new SupportTicket
        {
            UserId = userId,
            Email = email,
            Subject = subject,
            Message = message,
            Status = TicketStatus.Open
        };

        await _uow.Repository<SupportTicket>().AddAsync(ticket);
        await _uow.SaveChangesAsync();
        return ticket;
    }

    public async Task<SupportTicket?> GetByIdAsync(string id)
    {
        return await _uow.Repository<SupportTicket>().Query()
            .Include(t => t.User)
            .Include(t => t.Replies.OrderBy(r => r.CreatedAt))
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<SupportTicket>> GetUserTicketsAsync(string userId, int page = 1, int pageSize = 20)
    {
        return await _uow.Repository<SupportTicket>().Query()
            .Where(t => t.UserId == userId)
            .Include(t => t.Replies)
            .OrderByDescending(t => t.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetUserTicketCountAsync(string userId)
    {
        return await _uow.Repository<SupportTicket>().Query()
            .Where(t => t.UserId == userId)
            .CountAsync();
    }

    public async Task<List<SupportTicket>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        return await _uow.Repository<SupportTicket>().Query()
            .Include(t => t.User)
            .OrderByDescending(t => t.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync()
    {
        return await _uow.Repository<SupportTicket>().Query().CountAsync();
    }

    public async Task<SupportTicketReply> AddReplyAsync(string ticketId, string? userId, string message)
    {
        var reply = new SupportTicketReply
        {
            TicketId = ticketId,
            UserId = userId,
            Message = message
        };

        await _uow.Repository<SupportTicketReply>().AddAsync(reply);

        var ticket = await _uow.Repository<SupportTicket>().GetByIdAsync(ticketId);
        if (ticket != null)
        {
            ticket.UpdatedAt = DateTime.UtcNow;
            _uow.Repository<SupportTicket>().Update(ticket);
        }

        await _uow.SaveChangesAsync();
        return reply;
    }

    public async Task UpdateStatusAsync(string ticketId, TicketStatus status)
    {
        var ticket = await _uow.Repository<SupportTicket>().GetByIdAsync(ticketId);
        if (ticket == null) return;

        ticket.Status = status;
        ticket.UpdatedAt = DateTime.UtcNow;
        _uow.Repository<SupportTicket>().Update(ticket);
        await _uow.SaveChangesAsync();
    }

    public async Task<bool> IsOwnerAsync(string ticketId, string userId)
    {
        var ticket = await _uow.Repository<SupportTicket>().GetByIdAsync(ticketId);
        return ticket?.UserId == userId;
    }
}
