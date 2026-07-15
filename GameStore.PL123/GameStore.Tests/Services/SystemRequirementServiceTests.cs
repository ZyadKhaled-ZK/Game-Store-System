using FluentAssertions;

namespace GameStore.Tests.Services;

public class SystemRequirementServiceTests
{
    private readonly Mock<IJsonFileStore> _fileStoreMock;
    private readonly SystemRequirementService _service;
    private const string GameId = "game-1";

    public SystemRequirementServiceTests()
    {
        _fileStoreMock = new Mock<IJsonFileStore>();
        _service = new SystemRequirementService(_fileStoreMock.Object);
    }

    [Fact]
    public async Task GetAsync_ReturnsDataFromFileStore()
    {
        var model = new SystemRequirementsModel
        {
            Minimum = new RequirementModel { OS = "Windows 10", Processor = "i5", Memory = "8GB" },
            Recommended = new RequirementModel { OS = "Windows 11", Processor = "i7", Memory = "16GB" }
        };
        _fileStoreMock.Setup(f => f.ReadAsync<SystemRequirementsModel>($"requirements/{GameId}.json"))
            .ReturnsAsync(model);

        var result = await _service.GetAsync(GameId);

        result.Should().NotBeNull();
        result!.Minimum.OS.Should().Be("Windows 10");
        result.Recommended.Processor.Should().Be("i7");
    }

    [Fact]
    public async Task GetAsync_NoData_ReturnsNull()
    {
        _fileStoreMock.Setup(f => f.ReadAsync<SystemRequirementsModel>(It.IsAny<string>()))
            .ReturnsAsync((SystemRequirementsModel?)null);

        var result = await _service.GetAsync("new-game");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_WritesToCorrectPath()
    {
        var model = new SystemRequirementsModel
        {
            Minimum = new RequirementModel { OS = "Linux", Memory = "4GB" },
            Recommended = new RequirementModel { OS = "Linux", Memory = "8GB" }
        };

        await _service.SaveAsync(GameId, model);

        _fileStoreMock.Verify(f => f.WriteAsync(
            $"requirements/{GameId}.json", model), Times.Once);
    }

    [Theory]
    [InlineData("game-abc-123")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task SaveAsync_DifferentGameIds_CorrectPath(string gameId)
    {
        var model = new SystemRequirementsModel();

        await _service.SaveAsync(gameId, model);

        _fileStoreMock.Verify(f => f.WriteAsync(
            $"requirements/{gameId}.json", model), Times.Once);
    }
}
