namespace CommandRouter.Exceptions;

using System;

/// <summary>
/// Thrown when a command returns something other than void, <see cref="Results.ICommandResult"/>, or a Task or
/// ValueTask of either
/// </summary>
public class UnsupportedReturnTypeException(string command, Type returnType)
    : CommandRouterException($"Command '{command}' has unsupported return type '{returnType}'")
{
    public string Command { get; } = command;

    public Type ReturnType { get; } = returnType;
}
