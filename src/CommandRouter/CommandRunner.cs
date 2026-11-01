namespace CommandRouter;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Binding;
using Exceptions;
using Results;
using Routing;

internal class CommandRunner(
    ICommandTable commandTable,
    ICommandTokenizer commandTokenizer,
    ICommandSelector commandSelector,
    ParameterBinder parameterBinder)
    : ICommandRunner
{
    public async Task<ICommandResult> RunAsync(string command,
        IReadOnlyDictionary<string, object>? contextItems,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (!commandTokenizer.TryTokenize(command, out var tokenizedCommand, out var errorPosition))
        {
            throw new InvalidTokenException(
                $"Invalid token starting at position {errorPosition}",
                errorPosition);
        }

        if (!commandSelector.TrySelect(tokenizedCommand, commandTable, out var match))
            throw new CommandNotFoundException(command);

        var method = match.Method;
        var args = tokenizedCommand.GetArguments(match.ArgumentStart);

        var parameters = parameterBinder.BindParameters(method.Parameters, args);

        var context = new CommandContext
        {
            Arguments = args,
            CancellationToken = cancellationToken
        };

        //add extra items
        if (contextItems != null)
        {
            foreach (var conItems in contextItems)
                context.Items.TryAdd(conItems.Key, conItems.Value);
        }

        return await method.Action(parameters, context).ConfigureAwait(false);
    }
}
