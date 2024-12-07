namespace CommandRouter
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Binding;
    using Exceptions;
    using Results;
    using Routing;

    internal class CommandRunner : ICommandRunner
    {
        private readonly ICommandTable _commandTable;
        private readonly ICommandSelector _commandSelector;
        private readonly ParameterBinder _parameterBinder;

        public CommandRunner(ICommandTable commandTable, ICommandSelector commandSelector, ParameterBinder parameterBinder)
        {
            _commandTable = commandTable;
            _commandSelector = commandSelector;
            _parameterBinder = parameterBinder;
        }

        public Task<ICommandResult> RunAsync(string command)
        {
            return RunAsync(command, null);
        }

        public async Task<ICommandResult> RunAsync(string command, Dictionary<string, object>? contextItems)
        {
            ArgumentNullException.ThrowIfNull(command);

            if (!_commandSelector.TrySelectCommand(command, _commandTable, out var method, out var args))
                throw new CommandNotFoundException(command);

            var parameters = _parameterBinder.BindParameters(method.Parameters, args);

            var context = new CommandContext
            {
                Arguments = args
            };

            //add extra items
            if (contextItems != null)
            {
                foreach (var conItems in contextItems)
                {
                    if (!context.Items.ContainsKey(conItems.Key))
                        context.Items.Add(conItems.Key, conItems.Value);
                }
            }

            var result = method.Action(parameters, context);

            switch (result)
            {
                case ICommandResult commandResult:
                    return commandResult;
                case Task<ICommandResult> taskCommandResult:
                    return await taskCommandResult.ConfigureAwait(false);
                case Task taskResult:
                    await taskResult;
                    return EmptyResult.Empty;
                case null:
                    return EmptyResult.Empty;
                default:
                    throw new CommandRouterException("Failed to run command");
            }
        }
    }
}
