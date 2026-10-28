namespace CommandRouter;

using System;
using System.Threading.Tasks;
using Binding;
using Results;

public class CommandMethod
{
    public Guid Id { get; }
    public string Command { get; }

    internal Func<object?[], CommandContext, ValueTask<ICommandResult>> Action { get; }
    internal ParameterInfo[] Parameters { get; }

    internal CommandMethod(string command,
        Func<object?[], CommandContext, ValueTask<ICommandResult>> action,
        ParameterInfo[] parameters)
    {
        Id = Guid.NewGuid();
        Command = command;
        Action = action;
        Parameters = parameters;
    }
}