using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration.Bootstraps;
using Microsoft.Extensions.Hosting;

namespace EventFlow.AspNetCore.ServiceProvider
{
    /// <summary>
    ///     Ensures that the <see cref="Bootstrapper" /> is run in an ASP.NET Core
    ///     environment when EventFlow is configured into an existing ServiceCollection
    ///     instance and <see cref="CreateResolver" /> is not used.
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Local
    class HostedBootstrapper : IHostedService
    {
        private readonly IBootstrapper _bootstrapper;

        public HostedBootstrapper(IBootstrapper bootstrapper)
        {
            _bootstrapper = bootstrapper;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return _bootstrapper.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
