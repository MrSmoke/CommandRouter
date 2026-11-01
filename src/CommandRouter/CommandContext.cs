namespace CommandRouter;

using System.Collections.Generic;
using System.Threading;

public sealed class CommandContext
{
    public IDictionary<string, object> Items { get; } = new Dictionary<string, object>();
    public string[] Arguments { get; internal set; } = [];
    public CancellationToken CancellationToken { get; internal set; }
}
