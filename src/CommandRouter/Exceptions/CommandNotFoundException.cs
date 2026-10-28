namespace CommandRouter.Exceptions;

public class CommandNotFoundException(string commandName)
    : CommandRouterException($"Command {commandName} could not be found");