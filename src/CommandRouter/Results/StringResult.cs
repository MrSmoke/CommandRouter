namespace CommandRouter.Results;

using System.IO;
using System.Text;
using System.Threading.Tasks;

public class StringResult(string content, Encoding encoding) : ICommandResult
{
    public StringResult(string content) : this(content, Encoding.UTF8)
    {
    }

    public Task ExecuteAsync(Stream resultStream)
    {
        var bytes = encoding.GetBytes(content);
        return resultStream.WriteAsync(bytes, 0, bytes.Length);
    }
}