namespace CommandRouter.Results
{
    using System.IO;
    using System.Threading.Tasks;

    internal class EmptyResult : ICommandResult
    {
        public Task ExecuteAsync(Stream resultStream) => Task.CompletedTask;

        public static EmptyResult Empty => new();
    }
}
