namespace CommandRouter
{
    /// <summary>
    /// The result of selecting a command for a <see cref="TokenizedCommand"/>
    /// </summary>
    public readonly struct CommandMatch
    {
        public CommandMatch(CommandMethod method, int argumentStart)
        {
            Method = method;
            ArgumentStart = argumentStart;
        }

        /// <summary>
        /// The selected command
        /// </summary>
        public CommandMethod Method { get; }

        /// <summary>
        /// The index of the first token that is an argument. All tokens before it make up the command
        /// </summary>
        public int ArgumentStart { get; }
    }
}
