namespace CommandRouter;

/// <summary>
/// The result of selecting a command for a <see cref="TokenizedCommand"/>
/// </summary>
public readonly struct CommandMatch(CommandMethod method, int argumentStart)
{
    /// <summary>
    /// The selected command
    /// </summary>
    public CommandMethod Method { get; } = method;

    /// <summary>
    /// The index of the first token that is an argument. All tokens before it make up the command
    /// </summary>
    public int ArgumentStart { get; } = argumentStart;
}
