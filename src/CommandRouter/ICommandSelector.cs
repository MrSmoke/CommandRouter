namespace CommandRouter
{
    using Routing;

    public interface ICommandSelector
    {
        CommandMethod SelectCommand(string str, ICommandTable commandTable, out object[] extra);
    }
}
