namespace CommandRouter;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

internal sealed class DefaultCommandTokeniser : ICommandTokeniser
{
    public bool TryTokenise(string str, [NotNullWhen(true)] out TokenisedCommand? command,
        out int errorPosition)
    {
        ArgumentNullException.ThrowIfNull(str);

        errorPosition = -1;
        var result = new List<CommandToken>();

        var span = str.AsSpan();
        var index = IgnoreWhiteSpaces(span, 0);

        while (index < span.Length)
        {
            var tokenStart = index;

            if (span[index] == '\"')
            {
                var closingQuote = span[(index + 1)..].IndexOf('\"');

                if (closingQuote == -1)
                {
                    //missing closing quote, the string is invalid
                    command = null;
                    errorPosition = tokenStart;
                    return false;
                }

                //the quoted text is kept exactly as written, then skip the closing quote
                index += closingQuote + 2;
                result.Add(new CommandToken(str, tokenStart, index - tokenStart, true));
            }
            else
            {
                while (index < span.Length && span[index] != '\"' && !char.IsWhiteSpace(span[index]))
                    index++;

                result.Add(new CommandToken(str, tokenStart, index - tokenStart, false));
            }

            index = IgnoreWhiteSpaces(span, index);
        }

        command = new TokenisedCommand(str, result);
        return true;
    }

    private static int IgnoreWhiteSpaces(ReadOnlySpan<char> command, int index)
    {
        while (index < command.Length && char.IsWhiteSpace(command[index]))
            index++;

        return index;
    }
}
