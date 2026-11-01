namespace CommandRouter;

using System;
using System.Linq;
using Routing;

internal class DefaultCommandSelector : ICommandSelector
{
    /// <summary>
    /// Selects the command matching the most leading tokens, falling back to the root command
    /// </summary>
    public bool TrySelect(TokenizedCommand command, ICommandTable commandTable, out CommandMatch match)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(commandTable);

        var tokens = command.Tokens;

        for (var index = tokens.Count; index > -1; --index)
        {
            //index 0 is the root/default route
            var cmd = index == 0
                ? ""
                : string.Join(" ", tokens.Take(index).Select(t => t.Value));

            if (!commandTable.TryGetValue(cmd, out var method))
                continue;

            match = new CommandMatch(method, index);
            return true;
        }

        match = default;
        return false;
    }
}
