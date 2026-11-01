namespace CommandRouter;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Splits a command string into tokens. Register your own implementation to customise the command syntax.
/// </summary>
public interface ICommandTokeniser
{
    /// <summary>
    /// Splits a command string into tokens
    /// </summary>
    /// <param name="str">The command string</param>
    /// <param name="command">The tokenised command when successful, otherwise null</param>
    /// <param name="errorPosition">
    /// When tokenising fails, the zero based position in the command string where the invalid token starts.
    /// Otherwise -1.
    /// </param>
    /// <returns>true if the command string was tokenised, otherwise false</returns>
    bool TryTokenise(string str, [NotNullWhen(true)] out TokenisedCommand? command, out int errorPosition);
}