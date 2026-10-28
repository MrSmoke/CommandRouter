namespace CommandRouter
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Splits a command string into tokens. Register your own implementation to customise the command syntax.
    /// </summary>
    public interface ICommandTokenizer
    {
        /// <summary>
        /// Splits a command string into tokens
        /// </summary>
        /// <param name="str">The command string</param>
        /// <param name="command">The tokenized command when successful, otherwise null</param>
        /// <param name="errorPosition">
        /// When tokenizing fails, the zero based position in the command string where the invalid token starts.
        /// Otherwise -1.
        /// </param>
        /// <returns>true if the command string was tokenized, otherwise false</returns>
        bool TryTokenize(string str, [NotNullWhen(true)] out TokenizedCommand? command, out int errorPosition);
    }
}
