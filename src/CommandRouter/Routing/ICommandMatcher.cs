namespace CommandRouter.Routing;

using System.Collections.Generic;

/// <summary>
/// Implemented by command tables that can match tokens directly, without the caller building command strings
/// </summary>
internal interface ICommandMatcher
{
    bool TryMatch(IReadOnlyList<CommandToken> tokens, out CommandMatch match);
}
