namespace CommandRouter
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Results;

    public interface ICommandRunner
    {
        Task<ICommandResult> RunAsync(string command);
        Task<ICommandResult> RunAsync(string command, Dictionary<string, object> contextItems);
    }
}