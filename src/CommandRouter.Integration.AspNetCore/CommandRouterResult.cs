namespace CommandRouter.Integration.AspNetCore;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Results;

public class CommandRouterResult(ICommandResult commandResult) : IActionResult
{
    public Task ExecuteResultAsync(ActionContext context)
    {
        return commandResult.ExecuteAsync(context.HttpContext.Response.Body);
    }
}