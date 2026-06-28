using FluentAssertions;

namespace GameStore.Tests.Services;

public class ChatServiceTests
{
    private static GameStoreDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GameStoreDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GameStoreDbContext(options);
    }

    private static async Task SeedUsersAsync(GameStoreDbContext context)
    {
        context.Users.AddRange(
            new User { Id = "u1", Username = "Alice", Email = "a@test.com", PasswordHash = "h" },
            new User { Id = "u2", Username = "Bob", Email = "b@test.com", PasswordHash = "h" }
        );
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task SendMessageAsync_Creates_Message()
    {
        using var ctx = CreateContext("Chat_Send");
        await SeedUsersAsync(ctx);
        var uow = new UnitOfWork(ctx);
        var service = new ChatService(uow);

        var msg = await service.SendMessageAsync("u1", "u2", "Hello Bob!");

        msg.Should().NotBeNull();
        msg!.SenderId.Should().Be("u1");
        msg.ReceiverId.Should().Be("u2");
        msg.Content.Should().Be("Hello Bob!");
        msg.ReadAt.Should().BeNull();
    }

    [Fact]
    public async Task GetConversationAsync_Returns_Messages_Between_Two_Users()
    {
        using var ctx = CreateContext("Chat_Conversation");
        await SeedUsersAsync(ctx);
        ctx.Messages.AddRange(
            new Message { Id = "m1", SenderId = "u1", ReceiverId = "u2", Content = "Hi", SentAt = DateTime.UtcNow.AddMinutes(-5) },
            new Message { Id = "m2", SenderId = "u2", ReceiverId = "u1", Content = "Hey", SentAt = DateTime.UtcNow.AddMinutes(-4) },
            new Message { Id = "m3", SenderId = "u1", ReceiverId = "u2", Content = "How are you?", SentAt = DateTime.UtcNow.AddMinutes(-3) }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new ChatService(uow);

        var msgs = await service.GetConversationAsync("u1", "u2");

        msgs.Should().HaveCount(3);
        msgs.Should().BeInAscendingOrder(m => m.SentAt);
    }

    [Fact]
    public async Task GetConversationAsync_Returns_Empty_For_No_Messages()
    {
        using var ctx = CreateContext("Chat_EmptyConv");
        await SeedUsersAsync(ctx);
        var uow = new UnitOfWork(ctx);
        var service = new ChatService(uow);

        var msgs = await service.GetConversationAsync("u1", "u2");

        msgs.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUnreadCountAsync_Counts_Unread_Messages()
    {
        using var ctx = CreateContext("Chat_UnreadCount");
        await SeedUsersAsync(ctx);
        ctx.Messages.AddRange(
            new Message { Id = "m1", SenderId = "u2", ReceiverId = "u1", Content = "Hi", ReadAt = null },
            new Message { Id = "m2", SenderId = "u2", ReceiverId = "u1", Content = "Hello", ReadAt = null },
            new Message { Id = "m3", SenderId = "u2", ReceiverId = "u1", Content = "Read", ReadAt = DateTime.UtcNow }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new ChatService(uow);

        var count = await service.GetUnreadCountAsync("u1");

        count.Should().Be(2);
    }

    [Fact]
    public async Task MarkAsReadAsync_Marks_Unread_Messages_As_Read()
    {
        using var ctx = CreateContext("Chat_MarkRead");
        await SeedUsersAsync(ctx);
        ctx.Messages.AddRange(
            new Message { Id = "m1", SenderId = "u2", ReceiverId = "u1", Content = "Hi" },
            new Message { Id = "m2", SenderId = "u2", ReceiverId = "u1", Content = "Hello" }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new ChatService(uow);

        await service.MarkAsReadAsync("u2", "u1");

        ctx.Messages.ToList().All(m => m.ReadAt != null).Should().BeTrue();
    }

    [Fact]
    public async Task GetConversationsAsync_Returns_Contact_List()
    {
        using var ctx = CreateContext("Chat_Conversations");
        await SeedUsersAsync(ctx);
        ctx.Messages.Add(
            new Message { Id = "m1", SenderId = "u1", ReceiverId = "u2", Content = "Hey Bob", SentAt = DateTime.UtcNow }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new ChatService(uow);

        var conversations = await service.GetConversationsAsync("u1");

        conversations.Should().HaveCount(1);
        conversations[0].UserId.Should().Be("u2");
        conversations[0].Username.Should().Be("Bob");
        conversations[0].LastMessage.Should().NotBeNull();
        conversations[0].LastMessage!.Content.Should().Be("Hey Bob");
    }
}
