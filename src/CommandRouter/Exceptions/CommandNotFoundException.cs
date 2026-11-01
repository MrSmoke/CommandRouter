namespace CommandRouter.Exceptions;

public sealed class CommandNotFoundException(string commandName)
    : CommandRouterException($"Command {commandName} could not be found");