namespace CommandRouter;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Results;

public interface ICommandRunner
{
    /// <param name="command">The command string to run</param>
    /// <param name="contextItems">Copied to <see cref="CommandContext.Items"/></param>
    /// <param name="cancellationToken">Available on <see cref="CommandContext.CancellationToken"/></param>
    Task<ICommandResult> RunAsync(string command, IReadOnlyDictionary<string, object>? contextItems,
        CancellationToken cancellationToken);
}
