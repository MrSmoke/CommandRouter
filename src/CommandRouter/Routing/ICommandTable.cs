namespace CommandRouter.Routing;

using System;
using System.Collections.Generic;
using Results;

public interface ICommandTable : IReadOnlyDictionary<string, CommandMethod>
{
    /// <summary>
    /// Adds a command. Not thread-safe, so only call this at startup before any commands run
    /// </summary>
    void AddCommand(string command, Func<object?[], CommandContext, ICommandResult> action);
}
