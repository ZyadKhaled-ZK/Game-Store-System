using FluentAssertions;

namespace GameStore.Tests.Services;

public class SupportTicketServiceTests
{
    private static GameStoreDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GameStoreDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GameStoreDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_Creates_Ticket_With_Open_Status()
    {
        using var ctx = CreateContext("ST_Create");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new SupportTicketService(uow);

        var ticket = await service.CreateAsync("u1", null, "Help", "I need help");

        ticket.Id.Should().NotBeNull();
        ticket.Subject.Should().Be("Help");
        ticket.Message.Should().Be("I need help");
        ticket.Status.Should().Be(TicketStatus.Open);
        ticket.UserId.Should().Be("u1");
    }

    [Fact]
    public async Task CreateAsync_Creates_Anonymous_Ticket_With_Email()
    {
        using var ctx = CreateContext("ST_CreateAnon");
        var uow = new UnitOfWork(ctx);
        var service = new SupportTicketService(uow);

        var ticket = await service.CreateAsync(null, "anon@t.com", "Bug", "Found a bug");

        ticket.Id.Should().NotBeNull();
        ticket.Email.Should().Be("anon@t.com");
        ticket.UserId.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Ticket_With_Replies()
    {
        using var ctx = CreateContext("ST_GetById");
        ctx.Users.Add(new User { Id = "u1", Username = "A", Email = "a@t.com", PasswordHash = "h" });
        ctx.SupportTickets.Add(new SupportTicket { Id = "t1", UserId = "u1", Subject = "S", Message = "M", Status = TicketStatus.Open });
        ctx.SupportTicketReplies.Add(new SupportTicketReply { Id = "r1", TicketId = "t1", UserId = "u1", Message = "Reply" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new SupportTicketService(uow);

        var ticket = await service.GetByIdAsync("t1");

        ticket.Should().NotBeNull();
        ticket!.Replies.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_If_Not_Found()
    {
        using var ctx = CreateContext("ST_GetByIdNF");
        var uow = new UnitOfWork(ctx);
        var service = new SupportTicketService(uow);

        var ticket = await service.GetByIdAsync("nonexistent");

        ticket.Should().BeNull();
    }

    [Fact]
    public async Task GetUserTicketsAsync_Returns_Paginated_Tickets()
    {
        using var ctx = CreateContext("ST_UserTickets");
        ctx.Users.Add(new User { Id = "u1", Username = "A", Email = "a@t.com", PasswordHash = "h" });
        for (var i = 1; i <= 5; i++)
            ctx.SupportTickets.Add(new SupportTicket { Id = $"t{i}", UserId = "u1", Subject = $"S{i}", Message = "M", Status = TicketStatus.Open });
        ctx.SupportTickets.Add(new SupportTicket { Id = "t_other", UserId = "u2", Subject = "Other", Message = "M", Status = TicketStatus.Open });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new SupportTicketService(uow);

        var tickets = await service.GetUserTicketsAsync("u1", 1, 3);

        tickets.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetUserTicketCountAsync_Returns_Count()
    {
        using var ctx = CreateContext("ST_UserCount");
        ctx.Users.Add(new User { Id = "u1", Username = "A", Email = "a@t.com", PasswordHash = "h" });
        ctx.SupportTickets.AddRange(
            new SupportTicket { Id = "t1", UserId = "u1", Subject = "S1", Message = "M", Status = TicketStatus.Open },
            new SupportTicket { Id = "t2", UserId = "u1", Subject = "S2", Message = "M", Status = TicketStatus.Open }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new SupportTicketService(uow);

        var count = await service.GetUserTicketCountAsync("u1");

        count.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_Returns_Paginated_All_Tickets()
    {
        using var ctx = CreateContext("ST_GetAll");
        ctx.Users.Add(new User { Id = "u1", Username = "A", Email = "a@t.com", PasswordHash = "h" });
        for (var i = 1; i <= 5; i++)
            ctx.SupportTickets.Add(new SupportTicket { Id = $"t{i}", UserId = "u1", Subject = $"S{i}", Message = "M", Status = TicketStatus.Open });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new SupportTicketService(uow);

        var tickets = await service.GetAllAsync(1, 2);

        tickets.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCountAsync_Returns_Total()
    {
        using var ctx = CreateContext("ST_Count");
        ctx.SupportTickets.AddRange(
            new SupportTicket { Id = "t1", UserId = "u1", Subject = "S1", Message = "M", Status = TicketStatus.Open },
            new SupportTicket { Id = "t2", UserId = "u1", Subject = "S2", Message = "M", Status = TicketStatus.Open }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new SupportTicketService(uow);

        var count = await service.GetCountAsync();

        count.Should().Be(2);
    }

    [Fact]
    public async Task AddReplyAsync_Adds_Reply_And_Updates_Ticket_UpdatedAt()
    {
        using var ctx = CreateContext("ST_AddReply");
        ctx.Users.Add(new User { Id = "u1", Username = "A", Email = "a@t.com", PasswordHash = "h" });
        ctx.SupportTickets.Add(new SupportTicket { Id = "t1", UserId = "u1", Subject = "S", Message = "M", Status = TicketStatus.Open, CreatedAt = DateTime.UtcNow.AddDays(-1) });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new SupportTicketService(uow);

        var reply = await service.AddReplyAsync("t1", "u1", "My reply");

        reply.Should().NotBeNull();
        reply.Message.Should().Be("My reply");
        ctx.SupportTicketReplies.Should().HaveCount(1);
        ctx.SupportTickets.Find("t1")!.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateStatusAsync_Updates_Status()
    {
        using var ctx = CreateContext("ST_UpdateStatus");
        ctx.SupportTickets.Add(new SupportTicket { Id = "t1", UserId = "u1", Subject = "S", Message = "M", Status = TicketStatus.Open });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new SupportTicketService(uow);

        await service.UpdateStatusAsync("t1", TicketStatus.InProgress);

        ctx.SupportTickets.Find("t1")!.Status.Should().Be(TicketStatus.InProgress);
    }

    [Fact]
    public async Task IsOwnerAsync_Returns_True_If_Owner()
    {
        using var ctx = CreateContext("ST_IsOwner");
        ctx.SupportTickets.Add(new SupportTicket { Id = "t1", UserId = "u1", Subject = "S", Message = "M", Status = TicketStatus.Open });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new SupportTicketService(uow);

        var result = await service.IsOwnerAsync("t1", "u1");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsOwnerAsync_Returns_False_If_Not_Owner()
    {
        using var ctx = CreateContext("ST_IsOwnerNot");
        ctx.SupportTickets.Add(new SupportTicket { Id = "t1", UserId = "u1", Subject = "S", Message = "M", Status = TicketStatus.Open });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new SupportTicketService(uow);

        var result = await service.IsOwnerAsync("t1", "u2");

        result.Should().BeFalse();
    }
}
