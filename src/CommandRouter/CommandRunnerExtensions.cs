namespace CommandRouter;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Results;

public static class CommandRunnerExtensions
{
    /// <inheritdoc cref="ICommandRunner.RunAsync"/>
    public static Task<ICommandResult> RunAsync(this ICommandRunner runner, string command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(runner);

        return runner.RunAsync(command, null, cancellationToken);
    }

    /// <inheritdoc cref="ICommandRunner.RunAsync"/>
    public static Task<ICommandResult> RunAsync(this ICommandRunner runner, string command,
        IReadOnlyDictionary<string, object>? contextItems)
    {
        ArgumentNullException.ThrowIfNull(runner);

        return runner.RunAsync(command, contextItems, CancellationToken.None);
    }
}
