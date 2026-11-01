namespace CommandRouter.Tests.Binding;

using System;
using CommandRouter.Binding;
using CommandRouter.Converters;
using Exceptions;
using Moq;
using Xunit;

public class ParameterBinderTests
{
    [Fact]
    public void BindParameters_DefaultParameters()
    {
        ParameterInfo[] parameterInfos =
        [
            new()
            {
                Name = "param",
                Type = typeof(string),
                DefaultValue = "123",
                HasDefaultValue = true,
                IsNullable = false
            }
        ];

        var mockBinder = new Mock<IPropertyConverter>();

        var binder = TestBinder.Create(mockBinder.Object);

        var objs = binder.BindParameters(parameterInfos, []);

        Assert.NotEmpty(objs);
        Assert.Equal("123", objs[0]);
    }

    [Fact]
    public void BindParameters_FewerArgumentsThanParameters_UsesDefaultsForAllMissingParameters()
    {
        ParameterInfo[] parameterInfos =
        [
            new() { Name = "a", Type = typeof(string) },
            new() { Name = "b", Type = typeof(int), DefaultValue = 5, HasDefaultValue = true },
            new() { Name = "c", Type = typeof(int), DefaultValue = 7, HasDefaultValue = true }
        ];

        var binder = TestBinder.Create(CreatePassThroughConverter());

        var objs = binder.BindParameters(parameterInfos, ["x"]);

        Assert.Equal(3, objs.Length);
        Assert.Equal("x", objs[0]);
        Assert.Equal(5, objs[1]);
        Assert.Equal(7, objs[2]);
    }

    [Fact]
    public void BindParameters_MissingRequiredParameterAfterSuppliedArguments_Throws()
    {
        ParameterInfo[] parameterInfos =
        [
            new() { Name = "a", Type = typeof(string) },
            new() { Name = "b", Type = typeof(int), DefaultValue = 5, HasDefaultValue = true },
            new() { Name = "c", Type = typeof(int) }
        ];

        var binder = TestBinder.Create(CreatePassThroughConverter());

        var ex = Assert.Throws<ParameterBindingException>(() => binder.BindParameters(parameterInfos, ["x"]));
        Assert.Equal("c", ex.ParameterName);
        Assert.Null(ex.Value);
    }

    [Theory]
    [InlineData(typeof(int), "abc", typeof(FormatException))]
    [InlineData(typeof(byte), "300", typeof(OverflowException))]
    [InlineData(typeof(DayOfWeek), "someday", typeof(ArgumentException))]
    public void BindParameters_ConversionFails_ThrowsParameterBindingException(Type type, string value,
        Type innerType)
    {
        ParameterInfo[] parameterInfos = [new() { Name = "p", Type = type }];

        var binder = TestBinder.Create();

        var ex = Assert.Throws<ParameterBindingException>(() => binder.BindParameters(parameterInfos, [value]));
        Assert.Equal("p", ex.ParameterName);
        Assert.Equal(value, ex.Value);
        Assert.IsType(innerType, ex.InnerException);
    }

    [Fact]
    public void BindParameters_NoConverter_ThrowsParameterBindingException()
    {
        ParameterInfo[] parameterInfos = [new() { Name = "p", Type = typeof(Uri) }];

        var binder = TestBinder.Create();

        var ex = Assert.Throws<ParameterBindingException>(() => binder.BindParameters(parameterInfos, ["x"]));
        Assert.Equal("p", ex.ParameterName);
        Assert.Equal("x", ex.Value);
    }

    [Fact]
    public void BindParameters_ExtraArguments_Throws()
    {
        ParameterInfo[] parameterInfos = [new() { Name = "a", Type = typeof(string) }];

        var binder = TestBinder.Create();

        var ex = Assert.Throws<ParameterBindingException>(() => binder.BindParameters(parameterInfos, ["x", "y"]));
        Assert.Null(ex.ParameterName);
        Assert.Equal("y", ex.Value);
    }

    [Fact]
    public void BindParameters_ParamsArray_ReceivesRemainingArguments()
    {
        ParameterInfo[] parameterInfos =
        [
            new() { Name = "a", Type = typeof(string) },
            new() { Name = "rest", Type = typeof(string[]), IsParams = true }
        ];

        var binder = TestBinder.Create();

        var objs = binder.BindParameters(parameterInfos, ["x", "y", "z"]);

        Assert.Equal("x", objs[0]);
        Assert.Equal(["y", "z"], Assert.IsType<string[]>(objs[1]));
    }

    [Fact]
    public void BindParameters_ParamsArray_NoRemainingArguments_IsEmpty()
    {
        ParameterInfo[] parameterInfos = [new() { Name = "rest", Type = typeof(string[]), IsParams = true }];

        var binder = TestBinder.Create();

        var objs = binder.BindParameters(parameterInfos, []);

        Assert.Empty(Assert.IsType<string[]>(objs[0]));
    }

    [Fact]
    public void BindParameters_ParamsArray_ConvertsEachElement()
    {
        ParameterInfo[] parameterInfos = [new() { Name = "numbers", Type = typeof(int[]), IsParams = true }];

        var binder = TestBinder.Create();

        var objs = binder.BindParameters(parameterInfos, ["1", "2"]);

        Assert.Equal([1, 2], Assert.IsType<int[]>(objs[0]));
    }

    [Fact]
    public void BindParameters_ParamsArray_BadElement_ThrowsWithParameterName()
    {
        ParameterInfo[] parameterInfos = [new() { Name = "numbers", Type = typeof(int[]), IsParams = true }];

        var binder = TestBinder.Create();

        var ex = Assert.Throws<ParameterBindingException>(() => binder.BindParameters(parameterInfos, ["1", "x"]));
        Assert.Equal("numbers", ex.ParameterName);
        Assert.Equal("x", ex.Value);
    }

    private static IPropertyConverter CreatePassThroughConverter()
    {
        var converter = new Mock<IPropertyConverter>();
        converter.Setup(c => c.CanConvert(It.IsAny<Type>(), It.IsAny<string>())).Returns(true);
        converter.Setup(c => c.Convert(It.IsAny<Type>(), It.IsAny<string>())).Returns((Type _, string v) => v);
        return converter.Object;
    }
}