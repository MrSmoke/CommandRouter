namespace CommandRouter;

using Routing;

public interface ICommandSelector
{
    bool TrySelect(TokenizedCommand command, ICommandTable commandTable, out CommandMatch match);
}
