namespace CommandRouter.Exceptions;

public class InvalidTokenException(string message, int position) : CommandRouterException(message)
{
    public InvalidTokenException(string message) : this(message, -1)
    {
    }

    /// <summary>
    /// The zero based position in the command string where the invalid token starts, or -1 if unknown
    /// </summary>
    public int Position { get; } = position;
}
