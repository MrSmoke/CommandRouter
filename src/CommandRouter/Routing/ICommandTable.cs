namespace CommandRouter.Routing
{
    using System;
    using System.Collections.Generic;
    using Results;

    public interface ICommandTable : IReadOnlyDictionary<string, CommandMethod>
    {
        void AddCommand(string command, Func<object[], CommandContext, ICommandResult> action);
    }
}