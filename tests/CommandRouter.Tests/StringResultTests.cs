namespace CommandRouter.Tests;

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Results;
using Xunit;

public class StringResultTests
{
    [Fact]
    public async Task ExecuteAsync_WritesEncodedString()
    {
        using var stream = new MemoryStream();

        await new StringResult("héllo", Encoding.Unicode).ExecuteAsync(stream);

        Assert.Equal(Encoding.Unicode.GetBytes("héllo"), stream.ToArray());
    }

    [Fact]
    public async Task ExecuteAsync_Cancelled_Throws()
    {
        using var stream = new MemoryStream();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new StringResult("hello").ExecuteAsync(stream, new CancellationToken(true)));
    }
}
