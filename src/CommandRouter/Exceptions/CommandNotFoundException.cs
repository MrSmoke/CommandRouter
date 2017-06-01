namespace CommandRouter.Exceptions
{
    public class CommandNotFoundException : CommandRouterException
    {
        public CommandNotFoundException(string commandName) : base($"Command {commandName} could not be found")
        {
        }
    }
}
