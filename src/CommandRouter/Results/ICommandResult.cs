namespace CommandRouter.Results;

using System.IO;
using System.Threading;
using System.Threading.Tasks;

public interface ICommandResult
{
    Task ExecuteAsync(Stream resultStream, CancellationToken cancellationToken = default);
}