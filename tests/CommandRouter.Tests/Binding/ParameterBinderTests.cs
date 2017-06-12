namespace CommandRouter.Tests.Binding
{
    using System.Collections.Generic;
    using System.Reflection;
    using Moq;
    using Xunit;
    using CommandRouter.Converters;
    using CommandRouter.Binding;

    public class ParameterBinderTests
    {
        [Fact]
        public void BindParameters_DefaultParameters()
        {
            var parameterInfos = new List<CommandRouter.Binding.ParameterInfo>
            {
                new CommandRouter.Binding.ParameterInfo
                {
                    Name = "param",
                    Type = typeof(string),
                    DefaultValue = "123",
                    HasDefaultValue = true
                }
            };

            var mockBinder = new Mock<IPropertyConverter>();

            var binder = new ParameterBinder(new List<IPropertyConverter> {mockBinder.Object});

            IList<object> objs = binder.BindParameters(parameterInfos, new object[0]);

            Assert.NotEmpty(objs);
            Assert.Equal(objs[0], "123");
        }
    }
}
