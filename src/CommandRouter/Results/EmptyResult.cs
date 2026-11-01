namespace CommandRouter.Results;

using System.IO;
using System.Threading;
using System.Threading.Tasks;

internal sealed class EmptyResult : ICommandResult
{
    public Task ExecuteAsync(Stream resultStream, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public static readonly EmptyResult Empty = new();
}
