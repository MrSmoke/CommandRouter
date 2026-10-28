namespace CommandRouter.Tests.Binding
{
    using System;
    using System.Collections.Generic;
    using CommandRouter.Binding;
    using Converters;
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

            var binder = new ParameterBinder(new List<IPropertyConverter> { mockBinder.Object });

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

            var binder = new ParameterBinder(new List<IPropertyConverter> { CreatePassThroughConverter() });

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

            var binder = new ParameterBinder(new List<IPropertyConverter> { CreatePassThroughConverter() });

            var ex = Assert.Throws<ArgumentNullException>(() => binder.BindParameters(parameterInfos, ["x"]));
            Assert.Equal("c", ex.ParamName);
        }

        private static IPropertyConverter CreatePassThroughConverter()
        {
            var converter = new Mock<IPropertyConverter>();
            converter.Setup(c => c.CanConvert(It.IsAny<Type>(), It.IsAny<string>())).Returns(true);
            converter.Setup(c => c.Convert(It.IsAny<Type>(), It.IsAny<string>())).Returns((Type _, string v) => v);
            return converter.Object;
        }
    }
}
