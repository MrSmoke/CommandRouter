namespace CommandRouter;

using Routing;

public interface ICommandSelector
{
    bool TrySelect(TokenisedCommand command, ICommandTable commandTable, out CommandMatch match);
}
