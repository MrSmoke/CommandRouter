namespace CommandRouter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Binding;
    using Exceptions;
    using Results;
    using Routing;

    internal class CommandRunner : ICommandRunner
    {
        private readonly CommandTable _commandTable;
        private readonly ICommandSelector _commandSelector;
        private readonly ParameterBinder _parameterBinder;

        public CommandRunner(CommandTable commandTable, ICommandSelector commandSelector, ParameterBinder parameterBinder)
        {
            _commandTable = commandTable;
            _commandSelector = commandSelector;
            _parameterBinder = parameterBinder;
        }

        public Task<ICommandResult> RunAsync(string command)
        {
            return RunAsync(command, null);
        }

        public async Task<ICommandResult> RunAsync(string command, Dictionary<string, object> contextItems)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var method = GetMethod(command, out object[] args);

            if (method == null)
                throw new CommandNotFoundException(command);

            var parameters = GetParameters(method, args);

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

            try
            {
                var result = method.Action(parameters.ToArray(), context);

                if (method.ReturnType == typeof(Task<ICommandResult>))
                    return await ((Task<ICommandResult>)result).ConfigureAwait(false);

                if (method.ReturnType == typeof(ICommandResult))
                    return (ICommandResult) result;

                if (method.ReturnType == typeof(Task))
                {
                    await ((Task) result).ConfigureAwait(false);
                    return new EmptyResult();
                }

                if (method.ReturnType == typeof(void))
                    return new EmptyResult();

            }
            catch
            {
                //fine
            }

            throw new CommandRouterException("Failed to run command");
        }

        private CommandMethod GetMethod(string command, out object[] args)
        {
            return _commandSelector.SelectCommand(command, _commandTable, out args);
        }

        private IEnumerable<object> GetParameters(CommandMethod method, object[] arguments)
        {
            return _parameterBinder.BindParameters(method.Parameters, arguments);
        }
    }
}
