namespace CommandRouter;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

internal class DefaultCommandTokenizer : ICommandTokenizer
{
    public bool TryTokenize(string str, [NotNullWhen(true)] out TokenizedCommand? command,
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
                var length = span[index..].IndexOfAny(' ', '\"');
                index = length == -1 ? span.Length : index + length;
                result.Add(new CommandToken(str, tokenStart, index - tokenStart, false));
            }

            index = IgnoreWhiteSpaces(span, index);
        }

        command = new TokenizedCommand(str, result);
        return true;
    }

    private static int IgnoreWhiteSpaces(ReadOnlySpan<char> command, int index)
    {
        var next = command[index..].IndexOfAnyExcept(' ');

        return next == -1 ? command.Length : index + next;
    }
}
