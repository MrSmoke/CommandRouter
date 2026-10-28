namespace CommandRouter.Tests
{
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Extensions;
    using Microsoft.Extensions.DependencyInjection;
    using Routing;
    using Xunit;

    public class AddCommandRouterTests
    {
        private static readonly System.Reflection.Assembly TestAssembly = typeof(AddCommandRouterTests).Assembly;

        [Fact]
        public void AddCommandRouter_UserRegisteredSelectorBeforeCall_IsNotOverridden()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ICommandSelector, CustomSelector>();

            services.AddCommandRouter(TestAssembly);

            using var provider = services.BuildServiceProvider();
            Assert.IsType<CustomSelector>(provider.GetRequiredService<ICommandSelector>());
        }

        [Fact]
        public void AddCommandRouter_UserRegisteredTokenizerBeforeCall_IsNotOverridden()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ICommandTokenizer, CustomTokenizer>();

            services.AddCommandRouter(TestAssembly);

            using var provider = services.BuildServiceProvider();
            Assert.IsType<CustomTokenizer>(provider.GetRequiredService<ICommandTokenizer>());
        }

        [Fact]
        public void AddCommandRouter_CalledTwice_DoesNotDuplicateRegistrations()
        {
            var services = new ServiceCollection();

            services.AddCommandRouter(TestAssembly);
            var countAfterFirstCall = services.Count;
            services.AddCommandRouter(TestAssembly);

            Assert.Equal(countAfterFirstCall, services.Count);
            Assert.Single(services, d => d.ServiceType == typeof(ICommandRunner));
            Assert.Single(services, d => d.ServiceType == typeof(ICommandTable));
            Assert.Single(services, d => d.ServiceType == typeof(ApplicationManager));
        }

        [Fact]
        public void AddCommandRouter_RegistersCommandTableThroughFactory()
        {
            var services = new ServiceCollection();

            services.AddCommandRouter(TestAssembly);

            var descriptor = services.Single(d => d.ServiceType == typeof(ICommandTable));
            Assert.NotNull(descriptor.ImplementationFactory);
        }

        [Fact]
        public void AddCommandRouter_ReturnsSameServiceCollection()
        {
            var services = new ServiceCollection();

            Assert.Same(services, services.AddCommandRouter(TestAssembly));
            Assert.Same(services, services.AddCommandRouter(TestAssembly, _ => { }));
        }

        [Fact]
        public void ValidateCommandRouter_BuildsCommandTableAndReturnsProvider()
        {
            var services = new ServiceCollection();
            services.AddCommandRouter(TestAssembly);

            using var provider = services.BuildServiceProvider();

            Assert.Same(provider, provider.ValidateCommandRouter());
            Assert.True(provider.GetRequiredService<ICommandTable>().Count > 0);
        }

        [Fact]
        public void ApplicationManager_GetCommandFeature_ScansOnlyOnce()
        {
            var manager = new ApplicationManager();
            manager.Assemblies.Add(TestAssembly);

            var first = manager.GetCommandFeature();

            Assert.Same(first, manager.GetCommandFeature());
            Assert.Contains(first.Commands, t => t.AsType() == typeof(TestCommand));
        }

        private sealed class CustomSelector : ICommandSelector
        {
            public bool TrySelect(TokenizedCommand command, ICommandTable commandTable, out CommandMatch match)
            {
                match = default;
                return false;
            }
        }

        private sealed class CustomTokenizer : ICommandTokenizer
        {
            public bool TryTokenize(string command, [NotNullWhen(true)] out TokenizedCommand? tokenized,
                out int errorPosition)
            {
                tokenized = null;
                errorPosition = 0;
                return false;
            }
        }
    }
}
