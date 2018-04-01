namespace CommandRouter.Tests.Binding
{
    using System.Collections.Generic;
    using Moq;
    using Xunit;
    using Converters;
    using CommandRouter.Binding;

    public class ParameterBinderTests
    {
        [Fact]
        public void BindParameters_DefaultParameters()
        {
            var parameterInfos = new List<ParameterInfo>
            {
                new ParameterInfo
                {
                    Name = "param",
                    Type = typeof(string),
                    DefaultValue = "123",
                    HasDefaultValue = true
                }
            };

            var mockBinder = new Mock<IPropertyConverter>();

            var binder = new ParameterBinder(new List<IPropertyConverter> {mockBinder.Object});

            var objs = binder.BindParameters(parameterInfos, new object[0]);

            Assert.NotEmpty(objs);
            Assert.Equal("123", objs[0]);
        }
    }
}
