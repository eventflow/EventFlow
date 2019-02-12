using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.Extensions;
using EventFlow.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Extensions
{
    [Category(Categories.Unit)]
    public class BootstrapExtensionTests
    {
        [Test]
        public void ActionIsInvokedOnStartup()
        {
            bool hasBeenInvoked = false;

            EventFlowOptions.New
                .RunOnStartup(() => hasBeenInvoked = true)
                .CreateResolver(false);

            hasBeenInvoked.Should().BeTrue();
        }

        [Test]
        public void BootstrapIsInvokedOnStartup()
        {
            var check = EventFlowOptions.New
                .RegisterServices(s => s.Register<BootstrapCheck, BootstrapCheck>(Lifetime.Singleton))
                .RunOnStartup<TestBootstrap>()
                .CreateResolver(false)
                .Resolve<BootstrapCheck>();

            check.HasBeenInvoked.Should().BeTrue();
        }

        private class TestBootstrap : IBootstrap
        {
            private readonly BootstrapCheck _check;

            public TestBootstrap(BootstrapCheck check)
            {
                _check = check;
            }

            public Task BootAsync(CancellationToken cancellationToken)
            {
                _check.HasBeenInvoked = true;
                return Task.FromResult(true);
            }
        }

        private class BootstrapCheck
        {
            public bool HasBeenInvoked { get; set; }
        }
    }
}
