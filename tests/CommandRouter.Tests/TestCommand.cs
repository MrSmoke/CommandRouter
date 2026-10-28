namespace CommandRouter.Tests;

using System;
using Attributes;
using Commands;
using Converters;
using Results;
using Xunit;

#pragma warning disable CA1822
public class TestCommand : Command
{
    [Command("void-return-null-param")]
    public void VoidReturnNullParam(string? commandParam)
    {
        Assert.Null(commandParam);
    }

    [Command("void-return-param")]
    public void VoidReturnParam(string commandParam)
    {
        Assert.Equal("testvalue", commandParam);
    }

    [Command("void-return-int-param")]
    public void VoidReturnParam(int intParam)
    {
        Assert.Equal(999, intParam);
    }

    [Command("converter-custom")]
    public StringResult ConverterCustom(CustomValue value)
    {
        return new StringResult(value.Text);
    }
}

public class CustomValue
{
    public CustomValue(string text)
    {
        Text = text;
    }

    public string Text { get; }
}

public class CustomValueConverter : IPropertyConverter
{
    public bool CanConvert(Type propertyType, string value)
    {
        return propertyType == typeof(CustomValue);
    }

    public object Convert(Type propertyType, string value)
    {
        return new CustomValue("custom:" + value);
    }
}
