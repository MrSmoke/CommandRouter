namespace CommandRouter;

using System;
using System.Collections.Generic;

/// <summary>
/// A command string along with the tokens it was split into
/// </summary>
public sealed class TokenizedCommand
{
    public TokenizedCommand(string raw, IReadOnlyList<CommandToken> tokens)
    {
        ArgumentNullException.ThrowIfNull(raw);
        ArgumentNullException.ThrowIfNull(tokens);

        Raw = raw;
        Tokens = tokens;
    }

    /// <summary>
    /// The original command string
    /// </summary>
    public string Raw { get; }

    /// <summary>
    /// The tokens in the order they appear in <see cref="Raw"/>
    /// </summary>
    public IReadOnlyList<CommandToken> Tokens { get; }

    /// <summary>
    /// Gets the values of the tokens starting at <paramref name="start"/>
    /// </summary>
    public string[] GetArguments(int start)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(start, Tokens.Count);

        if (start == Tokens.Count)
            return [];

        var arguments = new string[Tokens.Count - start];
        for (var i = start; i < Tokens.Count; i++)
            arguments[i - start] = Tokens[i].Value;

        return arguments;
    }
}
