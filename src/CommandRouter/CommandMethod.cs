namespace CommandRouter
{
    using System;
    using Binding;

    public class CommandMethod
    {
        public Guid Id { get; }
        public string Command { get; }

        internal Func<object?[], CommandContext, object?> Action { get; }
        internal ParameterInfo[] Parameters { get; }

        internal CommandMethod(string command,
            Func<object?[], CommandContext, object?> action,
            ParameterInfo[] parameters)
        {
            Id = Guid.NewGuid();
            Command = command;
            Action = action;
            Parameters = parameters;
        }
    }
}
