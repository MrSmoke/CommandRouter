namespace CommandRouter
{
    using System.Collections.Generic;

    public class CommandContext
    {
        public IDictionary<string, object> Items { get; } = new Dictionary<string, object>();
        public string[] Arguments { get; internal set; } = [];
    }
}
