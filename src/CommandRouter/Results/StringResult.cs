namespace CommandRouter.Results;

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public sealed class StringResult(string content, Encoding encoding) : ICommandResult
{
    public StringResult(string content) : this(content, Encoding.UTF8)
    {
    }

    public Task ExecuteAsync(Stream resultStream, CancellationToken cancellationToken = default)
    {
        return resultStream.WriteAsync(encoding.GetBytes(content), cancellationToken).AsTask();
    }
}