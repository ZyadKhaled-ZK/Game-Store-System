using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;

namespace GameStore.Tests.Controllers;

public static class JsonResultExtensions
{
    private static JsonNode? ToJsonNode(this JsonResult result)
    {
        var json = JsonSerializer.Serialize(result.Value, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return JsonNode.Parse(json);
    }

    public static bool? GetJsonBool(this JsonResult result, string property)
        => result.ToJsonNode()?[property]?.GetValue<bool>();

    public static int? GetJsonInt(this JsonResult result, string property)
        => result.ToJsonNode()?[property]?.GetValue<int>();
}

internal static class TestAsyncQueryable
{
    public static IQueryable<T> BuildMock<T>(this IEnumerable<T> source) where T : class
        => new TestAsyncEnumerable<T>(source.AsQueryable());
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestAsyncEnumerable(Expression expression) : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;
    public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
    public T Current => _inner.Current;
    public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());
    public ValueTask DisposeAsync() { _inner.Dispose(); return new(); }
}

internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    public TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

    public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<TEntity>(expression);
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncEnumerable<TElement>(expression);
    public object? Execute(Expression expression) => _inner.Execute(expression);
    public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var result = _inner.Execute(expression);
        var actualResultType = typeof(TResult).GetGenericArguments()[0];
        var fromResult = typeof(Task).GetMethods()
            .First(m => m.Name == nameof(Task.FromResult) && m.IsGenericMethod)
            .MakeGenericMethod(actualResultType);
        return (TResult)fromResult.Invoke(null, [result])!;
    }
}

public static class TestHelper
{
    public static ControllerContext CreateControllerContext(string? userId = null, string? role = null, string? username = null)
    {
        var session = new MockHttpSession();
        if (userId != null) session.SetString("UserId", userId);
        if (role != null) session.SetString("Role", role);
        if (username != null) session.SetString("Username", username);

        var httpContext = new DefaultHttpContext { Session = session };
        return new ControllerContext { HttpContext = httpContext };
    }

    public static ITempDataDictionary CreateTempData()
    {
        var provider = new Mock<ITempDataProvider>();
        return new TempDataDictionary(new DefaultHttpContext(), provider.Object);
    }

    public static T SetupController<T>(T controller, string? userId = null, string? role = null, string? username = null) where T : Controller
    {
        controller.ControllerContext = CreateControllerContext(userId, role, username);
        controller.TempData = CreateTempData();
        return controller;
    }
}

public class MockHttpSession : ISession
{
    private readonly Dictionary<string, byte[]> _data = new(StringComparer.OrdinalIgnoreCase);

    public string Id => Guid.NewGuid().ToString();
    public bool IsAvailable => true;
    public IEnumerable<string> Keys => _data.Keys;

    public void Clear() => _data.Clear();
    public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken ct = default) => Task.CompletedTask;
    public void Remove(string key) => _data.Remove(key);
    public void Set(string key, byte[] value) => _data[key] = value;
    public bool TryGetValue(string key, out byte[]? value) => _data.TryGetValue(key, out value);
}
