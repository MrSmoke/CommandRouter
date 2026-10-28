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
        private readonly ICommandTokenizer _commandTokenizer;
        private readonly ICommandSelector _commandSelector;
        private readonly ParameterBinder _parameterBinder;

        public CommandRunner(ICommandTable commandTable, ICommandTokenizer commandTokenizer,
            ICommandSelector commandSelector, ParameterBinder parameterBinder)
        {
            _commandTable = commandTable;
            _commandTokenizer = commandTokenizer;
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

            if (!_commandTokenizer.TryTokenize(command, out var tokenizedCommand, out var errorPosition))
            {
                throw new InvalidTokenException(
                    $"Invalid token starting at position {errorPosition}",
                    errorPosition);
            }

            if (!_commandSelector.TrySelect(tokenizedCommand, _commandTable, out var match))
                throw new CommandNotFoundException(command);

            var method = match.Method;
            var args = tokenizedCommand.GetArguments(match.ArgumentStart);

            var parameters = _parameterBinder.BindParameters(method.Parameters, args);

            var context = new CommandContext
            {
                Arguments = args
            };

            //add extra items
            if (contextItems != null)
            {
                foreach (var conItems in contextItems)
                    context.Items.TryAdd(conItems.Key, conItems.Value);
            }

            return await method.Action(parameters, context).ConfigureAwait(false);
        }
    }
}
