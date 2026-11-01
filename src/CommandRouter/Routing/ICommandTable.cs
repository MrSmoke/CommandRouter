namespace CommandRouter.Routing;

using System.Collections.Generic;

public interface ICommandTable : IReadOnlyDictionary<string, CommandMethod>;
