using FluentAssertions;

namespace GameStore.Tests.Services;

public class JsonFileStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly Mock<IWebHostEnvironment> _envMock;
    private readonly JsonFileStore _store;

    public JsonFileStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "jsonfilestore_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _envMock = new Mock<IWebHostEnvironment>();
        _envMock.Setup(e => e.WebRootPath).Returns(_tempDir);
        _store = new JsonFileStore(_envMock.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task WriteAsync_ThenReadAsync_ReturnsSameData()
    {
        var data = new TestModel { Name = "Test", Value = 42 };

        await _store.WriteAsync("test/model.json", data);
        var result = await _store.ReadAsync<TestModel>("test/model.json");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task ReadAsync_NonExistentFile_ReturnsNull()
    {
        var result = await _store.ReadAsync<TestModel>("nonexistent/file.json");

        result.Should().BeNull();
    }

    [Fact]
    public async Task WriteAsync_CreatesNestedDirectories()
    {
        var data = new TestModel { Name = "Nested" };

        await _store.WriteAsync("a/b/c/deep.json", data);
        var result = await _store.ReadAsync<TestModel>("a/b/c/deep.json");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Nested");
    }

    [Fact]
    public async Task DeleteAsync_RemovesFile()
    {
        await _store.WriteAsync("to_delete.json", new TestModel { Name = "Delete Me" });

        await _store.DeleteAsync("to_delete.json");

        var result = await _store.ReadAsync<TestModel>("to_delete.json");
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentFile_DoesNotThrow()
    {
        var act = () => _store.DeleteAsync("ghost_file.json");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Exists_ReturnsTrueForExistingFile()
    {
        await _store.WriteAsync("exists.json", new TestModel { Name = "Here" });

        _store.Exists("exists.json").Should().BeTrue();
    }

    [Fact]
    public async Task Exists_ReturnsFalseForNonExistentFile()
    {
        _store.Exists("nope.json").Should().BeFalse();
    }

    [Fact]
    public async Task WriteAsync_OverwritesExistingFile()
    {
        await _store.WriteAsync("overwrite.json", new TestModel { Name = "V1" });
        await _store.WriteAsync("overwrite.json", new TestModel { Name = "V2" });

        var result = await _store.ReadAsync<TestModel>("overwrite.json");

        result!.Name.Should().Be("V2");
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\..\\windows\\system32")]
    [InlineData("/etc/passwd")]
    public void PathTraversal_ThrowsUnauthorizedAccessException(string maliciousPath)
    {
        var act = () => _store.ReadAsync<TestModel>(maliciousPath);

        act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task WriteAsync_WithSpecialCharactersInPath_HandlesCorrectly()
    {
        var data = new TestModel { Name = "Special" };

        await _store.WriteAsync("special/file-name_123.json", data);
        var result = await _store.ReadAsync<TestModel>("special/file-name_123.json");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Special");
    }

    [Fact]
    public async Task WriteAsync_HandlesForwardSlashSeparators()
    {
        var data = new TestModel { Name = "ForwardSlash" };

        await _store.WriteAsync("forward/slash/path.json", data);
        var result = await _store.ReadAsync<TestModel>("forward/slash/path.json");

        result.Should().NotBeNull();
        result!.Name.Should().Be("ForwardSlash");
    }

    [Fact]
    public async Task ReadAsync_DeserializesJsonWithCamelCase()
    {
        var data = new TestModel { Name = "Camel", Value = 99 };
        await _store.WriteAsync("camel.json", data);
        var result = await _store.ReadAsync<TestModel>("camel.json");

        result!.Name.Should().Be("Camel");
        result.Value.Should().Be(99);
    }

    [Fact]
    public async Task ConcurrentWrites_DoNotCorruptData()
    {
        var tasks = Enumerable.Range(0, 10).Select(i =>
            _store.WriteAsync($"concurrent_{i}.json", new TestModel { Name = $"Item{i}", Value = i }));

        await Task.WhenAll(tasks);

        for (int i = 0; i < 10; i++)
        {
            var result = await _store.ReadAsync<TestModel>($"concurrent_{i}.json");
            result.Should().NotBeNull();
            result!.Name.Should().Be($"Item{i}");
            result.Value.Should().Be(i);
        }
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
