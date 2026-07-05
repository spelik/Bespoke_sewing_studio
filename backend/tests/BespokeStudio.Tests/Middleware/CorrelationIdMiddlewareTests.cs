using BespokeStudio.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace BespokeStudio.Tests.Middleware;

public sealed class CorrelationIdMiddlewareTests
{
    private static CorrelationIdMiddleware CreateMiddleware() =>
        new(_ => Task.CompletedTask, NullLogger<CorrelationIdMiddleware>.Instance);

    [Fact]
    public async Task GeneratesCorrelationId_WhenHeaderMissing()
    {
        var context = new DefaultHttpContext();

        await CreateMiddleware().InvokeAsync(context);

        var responseId = context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        Assert.False(string.IsNullOrWhiteSpace(responseId));
        Assert.Equal(32, responseId.Length);
        Assert.Equal(responseId, context.Items[CorrelationIdMiddleware.HttpContextItemKey]);
    }

    [Fact]
    public async Task ReturnsTrimmedIncomingCorrelationId_WhenValid()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "  trace-123_abc:42.7  ";

        await CreateMiddleware().InvokeAsync(context);

        Assert.Equal(
            "trace-123_abc:42.7",
            context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
        Assert.Equal(
            "trace-123_abc:42.7",
            context.Items[CorrelationIdMiddleware.HttpContextItemKey]);
    }

    [Fact]
    public async Task ReplacesCorrelationId_WhenControlCharactersPresent()
    {
        var context = new DefaultHttpContext();
        const string unsafeValue = "abc\u0001def";
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = unsafeValue;

        await CreateMiddleware().InvokeAsync(context);

        var responseId = context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        Assert.NotEqual(unsafeValue, responseId);
        Assert.Equal(32, responseId.Length);
    }

    [Fact]
    public async Task ReplacesCorrelationId_WhenTooLong()
    {
        var context = new DefaultHttpContext();
        var tooLong = new string('a', 121);
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = tooLong;

        await CreateMiddleware().InvokeAsync(context);

        var responseId = context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        Assert.NotEqual(tooLong, responseId);
        Assert.Equal(32, responseId.Length);
    }

    [Fact]
    public async Task StoresResolvedCorrelationId_InHttpContextItems()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "valid-id";

        await CreateMiddleware().InvokeAsync(context);

        Assert.Equal("valid-id", context.Items[CorrelationIdMiddleware.HttpContextItemKey]);
    }
}
