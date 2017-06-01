namespace CommandRouter
{
    using System.Collections.Generic;
    using Routing;

    public interface ICommandSelector
    {
        CommandMethod SelectCommand(string str, CommandTable commandTable, out object[] extra);
    }
}
