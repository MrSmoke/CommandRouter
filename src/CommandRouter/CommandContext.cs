namespace CommandRouter
{
    using System.Collections.Generic;

    public class CommandContext
    {
        public IDictionary<string, object> Items { get; internal set; } = new Dictionary<string, object>();

        public IReadOnlyList<object> Arguments { get; internal set; } = new List<object>();
    }
}
