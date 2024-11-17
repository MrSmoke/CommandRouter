namespace CommandRouter
{
    using System.Diagnostics.CodeAnalysis;
    using Routing;

    public interface ICommandSelector
    {
        bool TrySelectCommand(string str, ICommandTable commandTable,
            [NotNullWhen(true)] out CommandMethod? method,
            [NotNullWhen(true)] out object[]? extra);
    }
}
