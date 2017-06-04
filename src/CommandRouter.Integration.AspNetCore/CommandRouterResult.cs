namespace CommandRouter.Integration.AspNetCore
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Results;

    public class CommandRouterResult : IActionResult
    {
        private readonly ICommandResult _commandResult;

        public CommandRouterResult(ICommandResult commandResult)
        {
            _commandResult = commandResult;
        }

        public Task ExecuteResultAsync(ActionContext context)
        {
            return _commandResult.ExecuteAsync(context.HttpContext.Response.Body);
        }
    }
}
