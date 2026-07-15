using FluentAssertions;

namespace GameStore.Tests.Services;

public class RefundServiceTests
{
    private readonly Mock<IJsonFileStore> _fileStoreMock;
    private readonly Mock<IOrderService> _orderServiceMock;
    private readonly Mock<ILibraryService> _libraryServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ICreditService> _creditServiceMock;
    private readonly RefundService _service;

    public RefundServiceTests()
    {
        _fileStoreMock = new Mock<IJsonFileStore>();
        _orderServiceMock = new Mock<IOrderService>();
        _libraryServiceMock = new Mock<ILibraryService>();
        _notificationServiceMock = new Mock<INotificationService>();
        _creditServiceMock = new Mock<ICreditService>();
        _service = new RefundService(
            _fileStoreMock.Object, _orderServiceMock.Object,
            _libraryServiceMock.Object, _notificationServiceMock.Object,
            _creditServiceMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_EmptyStore_ReturnsEmptyList()
    {
        _fileStoreMock.Setup(f => f.ReadAsync<List<RefundRequest>>(It.IsAny<string>()))
            .ReturnsAsync((List<RefundRequest>?)null);

        var result = await _service.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithRequests_ReturnsAll()
    {
        var requests = new List<RefundRequest>
        {
            new() { Id = "r1", UserId = "u1" },
            new() { Id = "r2", UserId = "u2" }
        };
        _fileStoreMock.Setup(f => f.ReadAsync<List<RefundRequest>>(It.IsAny<string>()))
            .ReturnsAsync(requests);

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsOnlyUserRequestsOrderedDescending()
    {
        var requests = new List<RefundRequest>
        {
            new() { Id = "r1", UserId = "u1", RequestedAt = DateTime.UtcNow.AddHours(-2) },
            new() { Id = "r2", UserId = "u2", RequestedAt = DateTime.UtcNow },
            new() { Id = "r3", UserId = "u1", RequestedAt = DateTime.UtcNow.AddHours(-1) }
        };
        _fileStoreMock.Setup(f => f.ReadAsync<List<RefundRequest>>(It.IsAny<string>()))
            .ReturnsAsync(requests);

        var result = await _service.GetByUserAsync("u1");

        result.Should().HaveCount(2);
        result[0].Id.Should().Be("r3");
        result[1].Id.Should().Be("r1");
    }

    [Fact]
    public async Task GetByIdAsync_Found_ReturnsRequest()
    {
        var requests = new List<RefundRequest>
        {
            new() { Id = "target", UserId = "u1", GameTitle = "Test Game" }
        };
        _fileStoreMock.Setup(f => f.ReadAsync<List<RefundRequest>>(It.IsAny<string>()))
            .ReturnsAsync(requests);

        var result = await _service.GetByIdAsync("target");

        result.Should().NotBeNull();
        result!.GameTitle.Should().Be("Test Game");
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        _fileStoreMock.Setup(f => f.ReadAsync<List<RefundRequest>>(It.IsAny<string>()))
            .ReturnsAsync(new List<RefundRequest>());

        var result = await _service.GetByIdAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task RequestAsync_OrderNotFound_ReturnsFailure()
    {
        _orderServiceMock.Setup(o => o.GetOrderByIdAsync("order-1"))
            .ReturnsAsync((Order?)null);

        var (success, message) = await _service.RequestAsync("order-1", "g1", "Bad game", "u1");

        success.Should().BeFalse();
        message.Should().Be("Order not found.");
    }

    [Fact]
    public async Task RequestAsync_DifferentUser_ReturnsFailure()
    {
        var order = new Order { Id = "order-1", UserId = "other-user", PaymentStatus = PaymentStatus.Completed, CreatedAt = DateTime.UtcNow };
        _orderServiceMock.Setup(o => o.GetOrderByIdAsync("order-1"))
            .ReturnsAsync(order);

        var (success, message) = await _service.RequestAsync("order-1", "g1", "Reason", "u1");

        success.Should().BeFalse();
        message.Should().Be("Order does not belong to you.");
    }

    [Fact]
    public async Task RequestAsync_IncompletePayment_ReturnsFailure()
    {
        var order = new Order { Id = "o1", UserId = "u1", PaymentStatus = PaymentStatus.Pending, CreatedAt = DateTime.UtcNow };
        _orderServiceMock.Setup(o => o.GetOrderByIdAsync("o1"))
            .ReturnsAsync(order);

        var (success, message) = await _service.RequestAsync("o1", "g1", "Reason", "u1");

        success.Should().BeFalse();
        message.Should().Be("Order cannot be refunded.");
    }

    [Fact]
    public async Task RequestAsync_ExpiredRefundWindow_ReturnsFailure()
    {
        var order = new Order { Id = "o1", UserId = "u1", PaymentStatus = PaymentStatus.Completed, CreatedAt = DateTime.UtcNow.AddDays(-15) };
        _orderServiceMock.Setup(o => o.GetOrderByIdAsync("o1"))
            .ReturnsAsync(order);

        var (success, message) = await _service.RequestAsync("o1", "g1", "Late", "u1");

        success.Should().BeFalse();
        message.Should().Contain("14 days");
    }

    [Fact]
    public async Task RequestAsync_GameNotInOrder_ReturnsFailure()
    {
        var order = new Order
        {
            Id = "o1", UserId = "u1", PaymentStatus = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            OrderItems = new List<OrderItem>
            {
                new() { GameId = "other-game", PriceAtPurchase = 10m, Game = new Game { Title = "Other" } }
            }
        };
        _orderServiceMock.Setup(o => o.GetOrderByIdAsync("o1")).ReturnsAsync(order);

        var (success, message) = await _service.RequestAsync("o1", "missing-game", "Reason", "u1");

        success.Should().BeFalse();
        message.Should().Be("Game not found in order.");
    }

    [Fact]
    public async Task RequestAsync_FreeGame_ReturnsFailure()
    {
        var order = new Order
        {
            Id = "o1", UserId = "u1", PaymentStatus = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            OrderItems = new List<OrderItem>
            {
                new() { GameId = "g1", PriceAtPurchase = 0m, Game = new Game { Title = "Free Game" } }
            }
        };
        _orderServiceMock.Setup(o => o.GetOrderByIdAsync("o1")).ReturnsAsync(order);

        var (success, message) = await _service.RequestAsync("o1", "g1", "Reason", "u1");

        success.Should().BeFalse();
        message.Should().Contain("free");
    }

    [Fact]
    public async Task RequestAsync_ValidRequest_CreatesRequestAndNotifiesAdmins()
    {
        var order = new Order
        {
            Id = "o1", UserId = "u1", PaymentStatus = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow, User = new User { Username = "TestUser" },
            OrderItems = new List<OrderItem>
            {
                new() { GameId = "g1", PriceAtPurchase = 29.99m, Game = new Game { Title = "Cool Game" } }
            }
        };
        _orderServiceMock.Setup(o => o.GetOrderByIdAsync("o1")).ReturnsAsync(order);
        _fileStoreMock.Setup(f => f.ReadAsync<List<RefundRequest>>(It.IsAny<string>()))
            .ReturnsAsync(new List<RefundRequest>());

        var (success, message) = await _service.RequestAsync("o1", "g1", "Don't like it", "u1");

        success.Should().BeTrue();
        message.Should().Be("Refund requested.");
        _fileStoreMock.Verify(f => f.WriteAsync(It.IsAny<string>(), It.IsAny<List<RefundRequest>>()), Times.Once);
        _notificationServiceMock.Verify(n => n.SendToAdminsAsync(
            It.IsAny<string>(), It.IsAny<string>(), "refund", null, It.IsAny<string>(), "/Admin/Refunds"), Times.Once);
    }

    [Fact]
    public async Task ApproveAsync_NotFound_ReturnsFailure()
    {
        _fileStoreMock.Setup(f => f.ReadAsync<List<RefundRequest>>(It.IsAny<string>()))
            .ReturnsAsync(new List<RefundRequest>());

        var (success, message) = await _service.ApproveAsync("nonexistent", "note");

        success.Should().BeFalse();
        message.Should().Contain("not found");
    }

    [Fact]
    public async Task ApproveAsync_NotPending_ReturnsFailure()
    {
        var requests = new List<RefundRequest>
        {
            new() { Id = "r1", Status = RefundStatus.Approved }
        };
        _fileStoreMock.Setup(f => f.ReadAsync<List<RefundRequest>>(It.IsAny<string>()))
            .ReturnsAsync(requests);

        var (success, message) = await _service.ApproveAsync("r1", "note");

        success.Should().BeFalse();
        message.Should().Contain("not in pending state");
    }

    [Fact]
    public async Task ApproveAsync_PendingRequest_ApprovesAndAddsCredit()
    {
        var requests = new List<RefundRequest>
        {
            new() { Id = "r1", UserId = "u1", GameTitle = "Game", Amount = 25m, OrderId = "o1", Status = RefundStatus.Pending }
        };
        _fileStoreMock.Setup(f => f.ReadAsync<List<RefundRequest>>(It.IsAny<string>()))
            .ReturnsAsync(requests);

        var (success, message) = await _service.ApproveAsync("r1", "Approved");

        success.Should().BeTrue();
        message.Should().Contain("store credit added");
        _creditServiceMock.Verify(c => c.AddCreditAsync("u1", 25m, It.IsAny<string>()), Times.Once);
        _notificationServiceMock.Verify(n => n.SendToUserAsync(
            "u1", It.IsAny<string>(), It.IsAny<string>(), "refund", It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ApproveAsync_CreditServiceThrows_RollsBackStatus()
    {
        var requests = new List<RefundRequest>
        {
            new() { Id = "r1", UserId = "u1", GameTitle = "Game", Amount = 25m, OrderId = "o1", Status = RefundStatus.Pending }
        };
        _fileStoreMock.Setup(f => f.ReadAsync<List<RefundRequest>>(It.IsAny<string>()))
            .ReturnsAsync(requests);
        _creditServiceMock.Setup(c => c.AddCreditAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("DB connection lost"));

        var (success, message) = await _service.ApproveAsync("r1", "note");

        success.Should().BeFalse();
        message.Should().Contain("DB connection lost");
        _notificationServiceMock.Verify(n => n.SendToAdminsAsync(
            It.IsAny<string>(), It.IsAny<string>(), "refund", null, "r1", null), Times.Once);
    }

    [Fact]
    public async Task RejectAsync_NotFound_ReturnsFailure()
    {
        _fileStoreMock.Setup(f => f.ReadAsync<List<RefundRequest>>(It.IsAny<string>()))
            .ReturnsAsync(new List<RefundRequest>());

        var (success, _) = await _service.RejectAsync("ghost", "Rejected");

        success.Should().BeFalse();
    }

    [Fact]
    public async Task RejectAsync_PendingRequest_RejectsAndNotifiesUser()
    {
        var requests = new List<RefundRequest>
        {
            new() { Id = "r2", UserId = "u1", GameTitle = "Game", Status = RefundStatus.Pending }
        };
        _fileStoreMock.Setup(f => f.ReadAsync<List<RefundRequest>>(It.IsAny<string>()))
            .ReturnsAsync(requests);

        var (success, message) = await _service.RejectAsync("r2", "Policy violation");

        success.Should().BeTrue();
        message.Should().Be("Refund rejected.");
        requests[0].Status.Should().Be(RefundStatus.Rejected);
        requests[0].ResolvedAt.Should().NotBeNull();
        _notificationServiceMock.Verify(n => n.SendToUserAsync(
            "u1", It.IsAny<string>(), It.IsAny<string>(), "refund", It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }
}
