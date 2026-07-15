using FluentAssertions;

namespace GameStore.Tests.Services;

public class CreditServiceTests
{
    private readonly Mock<IJsonFileStore> _fileStoreMock;
    private readonly CreditService _service;
    private const string UserId = "user-1";

    public CreditServiceTests()
    {
        _fileStoreMock = new Mock<IJsonFileStore>();
        _service = new CreditService(_fileStoreMock.Object);
    }

    [Fact]
    public async Task GetBalanceAsync_NewUser_ReturnsZero()
    {
        _fileStoreMock.Setup(f => f.ReadAsync<CreditLedger>(It.IsAny<string>()))
            .ReturnsAsync((CreditLedger?)null);

        var balance = await _service.GetBalanceAsync(UserId);

        balance.Should().Be(0m);
    }

    [Fact]
    public async Task GetBalanceAsync_WithEntries_SumsNonReservedAmounts()
    {
        var ledger = new CreditLedger
        {
            Entries = new List<CreditEntry>
            {
                new() { Amount = 50m, Type = "refund", CreatedAt = DateTime.UtcNow },
                new() { Amount = -10m, Type = "purchase", CreatedAt = DateTime.UtcNow },
                new() { Amount = -20m, Type = "reserved", CreatedAt = DateTime.UtcNow }
            }
        };
        _fileStoreMock.Setup(f => f.ReadAsync<CreditLedger>(It.IsAny<string>()))
            .ReturnsAsync(ledger);

        var balance = await _service.GetBalanceAsync(UserId);

        balance.Should().Be(40m);
    }

    [Fact]
    public async Task GetAvailableBalanceAsync_IncludesReservedEntries()
    {
        var ledger = new CreditLedger
        {
            Entries = new List<CreditEntry>
            {
                new() { Amount = 50m, Type = "refund", CreatedAt = DateTime.UtcNow },
                new() { Amount = -20m, Type = "reserved", CreatedAt = DateTime.UtcNow }
            }
        };
        _fileStoreMock.Setup(f => f.ReadAsync<CreditLedger>(It.IsAny<string>()))
            .ReturnsAsync(ledger);

        var available = await _service.GetAvailableBalanceAsync(UserId);

        available.Should().Be(30m);
    }

    [Fact]
    public async Task GetEntriesAsync_ReturnsOrderedByCreatedAtDescending()
    {
        var ledger = new CreditLedger
        {
            Entries = new List<CreditEntry>
            {
                new() { Amount = 10m, Type = "refund", CreatedAt = DateTime.UtcNow.AddHours(-2) },
                new() { Amount = 20m, Type = "refund", CreatedAt = DateTime.UtcNow },
                new() { Amount = 5m, Type = "refund", CreatedAt = DateTime.UtcNow.AddHours(-1) }
            }
        };
        _fileStoreMock.Setup(f => f.ReadAsync<CreditLedger>(It.IsAny<string>()))
            .ReturnsAsync(ledger);

        var entries = await _service.GetEntriesAsync(UserId);

        entries.Should().HaveCount(3);
        entries[0].Amount.Should().Be(20m);
        entries[1].Amount.Should().Be(5m);
        entries[2].Amount.Should().Be(10m);
    }

    [Fact]
    public async Task AddCreditAsync_AppendsEntryToLedger()
    {
        _fileStoreMock.Setup(f => f.ReadAsync<CreditLedger>(It.IsAny<string>()))
            .ReturnsAsync(new CreditLedger { Entries = new List<CreditEntry>() });
        CreditLedger? saved = null;
        _fileStoreMock.Setup(f => f.WriteAsync(It.IsAny<string>(), It.IsAny<CreditLedger>()))
            .Callback<string, CreditLedger>((_, l) => saved = l)
            .Returns(Task.CompletedTask);

        await _service.AddCreditAsync(UserId, 25.50m, "Test refund");

        saved.Should().NotBeNull();
        saved!.Entries.Should().HaveCount(1);
        saved.Entries[0].Amount.Should().Be(25.50m);
        saved.Entries[0].Type.Should().Be("refund");
        saved.Entries[0].Reason.Should().Be("Test refund");
    }

    [Fact]
    public async Task ReserveAsync_SufficientBalance_ReturnsSuccess()
    {
        var ledger = new CreditLedger
        {
            Entries = new List<CreditEntry>
            {
                new() { Amount = 100m, Type = "refund", CreatedAt = DateTime.UtcNow }
            }
        };
        _fileStoreMock.Setup(f => f.ReadAsync<CreditLedger>(It.IsAny<string>()))
            .ReturnsAsync(ledger);

        var (success, message) = await _service.ReserveAsync(UserId, 30m, "session-abc");

        success.Should().BeTrue();
        message.Should().Be("Credit reserved.");
    }

    [Fact]
    public async Task ReserveAsync_InsufficientBalance_ReturnsFailure()
    {
        var ledger = new CreditLedger
        {
            Entries = new List<CreditEntry>
            {
                new() { Amount = 10m, Type = "refund", CreatedAt = DateTime.UtcNow }
            }
        };
        _fileStoreMock.Setup(f => f.ReadAsync<CreditLedger>(It.IsAny<string>()))
            .ReturnsAsync(ledger);

        var (success, message) = await _service.ReserveAsync(UserId, 50m, "session-abc");

        success.Should().BeFalse();
        message.Should().Contain("Insufficient store credit");
    }

    [Fact]
    public async Task ReserveAsync_ExactBalance_ReturnsSuccess()
    {
        var ledger = new CreditLedger
        {
            Entries = new List<CreditEntry>
            {
                new() { Amount = 50m, Type = "refund", CreatedAt = DateTime.UtcNow }
            }
        };
        _fileStoreMock.Setup(f => f.ReadAsync<CreditLedger>(It.IsAny<string>()))
            .ReturnsAsync(ledger);

        var (success, _) = await _service.ReserveAsync(UserId, 50m, "session-exact");

        success.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmReservationAsync_ValidReservation_ConfirmsAndRemovesReservation()
    {
        var ledger = new CreditLedger
        {
            Entries = new List<CreditEntry>
            {
                new() { Amount = 100m, Type = "refund", CreatedAt = DateTime.UtcNow },
                new() { Amount = -30m, Type = "reserved", StripeSessionId = "sess-1", CreatedAt = DateTime.UtcNow }
            }
        };
        CreditLedger? saved = null;
        _fileStoreMock.Setup(f => f.ReadAsync<CreditLedger>(It.IsAny<string>()))
            .ReturnsAsync(ledger);
        _fileStoreMock.Setup(f => f.WriteAsync(It.IsAny<string>(), It.IsAny<CreditLedger>()))
            .Callback<string, CreditLedger>((_, l) => saved = l)
            .Returns(Task.CompletedTask);

        var (success, message) = await _service.ConfirmReservationAsync(UserId, "sess-1", "Purchase confirmed");

        success.Should().BeTrue();
        message.Should().Be("Reservation confirmed.");
        saved!.Entries.Should().NotContain(e => e.Type == "reserved");
        saved.Entries.Should().ContainSingle(e => e.Type == "purchase" && e.StripeSessionId == "sess-1");
    }

    [Fact]
    public async Task ConfirmReservationAsync_AlreadyConfirmed_ReturnsIdempotent()
    {
        var ledger = new CreditLedger
        {
            Entries = new List<CreditEntry>
            {
                new() { Amount = -30m, Type = "purchase", StripeSessionId = "sess-1", CreatedAt = DateTime.UtcNow }
            }
        };
        _fileStoreMock.Setup(f => f.ReadAsync<CreditLedger>(It.IsAny<string>()))
            .ReturnsAsync(ledger);

        var (success, message) = await _service.ConfirmReservationAsync(UserId, "sess-1", "Reason");

        success.Should().BeTrue();
        message.Should().Be("Already confirmed.");
    }

    [Fact]
    public async Task ConfirmReservationAsync_NoReservation_ReturnsFailure()
    {
        var ledger = new CreditLedger { Entries = new List<CreditEntry>() };
        _fileStoreMock.Setup(f => f.ReadAsync<CreditLedger>(It.IsAny<string>()))
            .ReturnsAsync(ledger);

        var (success, message) = await _service.ConfirmReservationAsync(UserId, "nonexistent-sess", "Reason");

        success.Should().BeFalse();
        message.Should().Contain("Reservation not found");
    }

    [Fact]
    public async Task ReleaseReservationAsync_RemovesReservationEntry()
    {
        var ledger = new CreditLedger
        {
            Entries = new List<CreditEntry>
            {
                new() { Amount = 50m, Type = "refund", CreatedAt = DateTime.UtcNow },
                new() { Amount = -20m, Type = "reserved", StripeSessionId = "sess-release", CreatedAt = DateTime.UtcNow }
            }
        };
        CreditLedger? saved = null;
        _fileStoreMock.Setup(f => f.ReadAsync<CreditLedger>(It.IsAny<string>()))
            .ReturnsAsync(ledger);
        _fileStoreMock.Setup(f => f.WriteAsync(It.IsAny<string>(), It.IsAny<CreditLedger>()))
            .Callback<string, CreditLedger>((_, l) => saved = l)
            .Returns(Task.CompletedTask);

        await _service.ReleaseReservationAsync(UserId, "sess-release");

        saved!.Entries.Should().NotContain(e => e.Type == "reserved" && e.StripeSessionId == "sess-release");
    }

    [Fact]
    public async Task ReleaseReservationAsync_NonexistentReservation_DoesNotThrow()
    {
        var ledger = new CreditLedger { Entries = new List<CreditEntry>() };
        _fileStoreMock.Setup(f => f.ReadAsync<CreditLedger>(It.IsAny<string>()))
            .ReturnsAsync(ledger);

        var act = () => _service.ReleaseReservationAsync(UserId, "ghost-session");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ReserveAsync_ZeroBalance_ReturnsFailure()
    {
        var ledger = new CreditLedger { Entries = new List<CreditEntry>() };
        _fileStoreMock.Setup(f => f.ReadAsync<CreditLedger>(It.IsAny<string>()))
            .ReturnsAsync(ledger);

        var (success, _) = await _service.ReserveAsync(UserId, 10m, "sess-zero");

        success.Should().BeFalse();
    }

    [Fact]
    public async Task GetBalanceAsync_EmptyLedger_ReturnsZero()
    {
        var ledger = new CreditLedger { Entries = new List<CreditEntry>() };
        _fileStoreMock.Setup(f => f.ReadAsync<CreditLedger>(It.IsAny<string>()))
            .ReturnsAsync(ledger);

        var balance = await _service.GetBalanceAsync(UserId);

        balance.Should().Be(0m);
    }
}
