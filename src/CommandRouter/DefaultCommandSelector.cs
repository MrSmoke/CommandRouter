namespace CommandRouter;

using System;
using System.Linq;
using Routing;

internal class DefaultCommandSelector : ICommandSelector
{
    public bool TrySelect(TokenizedCommand command, ICommandTable commandTable, out CommandMatch match)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(commandTable);

        // The default command table can match the tokens directly
        if (commandTable is ICommandMatcher matcher)
            return matcher.TryMatch(command.Tokens, out match);

        return TrySelectFromDictionary(command, commandTable, out match);
    }

    // Fallback for custom command tables: try the longest command first, then shorter ones
    private static bool TrySelectFromDictionary(TokenizedCommand command, ICommandTable commandTable,
        out CommandMatch match)
    {
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
