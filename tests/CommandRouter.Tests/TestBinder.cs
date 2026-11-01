namespace CommandRouter.Tests;

using CommandRouter.Binding;
using CommandRouter.Converters;
using Extensions;
using Microsoft.Extensions.Options;

internal static class TestBinder
{
    public static ParameterBinder Create(params IPropertyConverter[] converters)
    {
        var options = new CommandRouterOptions();
        options.PropertyConverters.AddRange(converters);

        return new ParameterBinder(Options.Create(options));
    }
}
