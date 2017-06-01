namespace CommandRouter
{
    using System;
    using System.Collections.Generic;
    using Binding;

    public class CommandMethod
    {
        public Guid Id;

        internal Func<object[], CommandContext, object> Action { get; set; }

        public string Command { get; internal set; }

        internal IList<ParameterInfo> Parameters = new List<ParameterInfo>();

        internal Type ReturnType { get; set; }

        public CommandMethod()
        {
            Id = Guid.NewGuid();
        }
    }
}
