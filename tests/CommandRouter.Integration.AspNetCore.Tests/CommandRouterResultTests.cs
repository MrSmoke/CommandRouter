namespace CommandRouter.Integration.AspNetCore.Tests;

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Results;
using Xunit;

public class CommandRouterResultTests
{
    [Fact]
    public async Task WritesResultToBody()
    {
        var httpContext = CreateHttpContext();

        await ExecuteAsync(new StringResult("hello"), httpContext);

        Assert.Equal("hello", Encoding.UTF8.GetString(((MemoryStream)httpContext.Response.Body).ToArray()));
    }

    [Fact]
    public async Task LeavesContentTypeUnset()
    {
        var httpContext = CreateHttpContext();

        await ExecuteAsync(new StringResult("hello"), httpContext);

        Assert.Null(httpContext.Response.ContentType);
    }

    [Fact]
    public async Task KeepsContentTypeSetByCaller()
    {
        var httpContext = CreateHttpContext();
        httpContext.Response.ContentType = "application/json";

        await ExecuteAsync(new StringResult("{}"), httpContext);

        Assert.Equal("application/json", httpContext.Response.ContentType);
    }

    [Fact]
    public async Task PassesRequestAbortedToResult()
    {
        using var cts = new CancellationTokenSource();
        var httpContext = CreateHttpContext();
        httpContext.RequestAborted = cts.Token;
        var result = new TokenCapturingResult();

        await ExecuteAsync(result, httpContext);

        Assert.Equal(cts.Token, result.Token);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        return httpContext;
    }

    private static Task ExecuteAsync(ICommandResult result, HttpContext httpContext)
    {
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new CommandRouterResult(result).ExecuteResultAsync(actionContext);
    }

    private sealed class TokenCapturingResult : ICommandResult
    {
        public CancellationToken Token { get; private set; }

        public Task ExecuteAsync(Stream resultStream, CancellationToken cancellationToken = default)
        {
            Token = cancellationToken;
            return Task.CompletedTask;
        }
    }
}
