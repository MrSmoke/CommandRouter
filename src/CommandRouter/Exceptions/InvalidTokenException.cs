namespace CommandRouter.Exceptions;

public class InvalidTokenException(string message, int position) : CommandRouterException(message)
{
    /// <summary>
    /// The zero based position in the command string where the invalid token starts
    /// </summary>
    public int Position { get; } = position;
}
