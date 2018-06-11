using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace EventFlow.Configuration.Bootstraps
{
    public class Bootstrapper : IBootstrapper
    {
        private readonly IEnumerable<IBootstrap> _bootstraps;
        public bool HasBeenRun { get; private set; }

        public Bootstrapper(IEnumerable<IBootstrap> bootstraps)
        {
            _bootstraps = bootstraps;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (HasBeenRun)
                return;

            HasBeenRun = true;

            var orderedBootstraps = OrderBootstraps(_bootstraps);
            foreach (var bootstrap in orderedBootstraps)
            {
                await bootstrap.BootAsync(cancellationToken);
            }
        }

        private static IReadOnlyCollection<IBootstrap> OrderBootstraps(IEnumerable<IBootstrap> bootstraps)
        {
            var list = bootstraps
                .Select(b => new
                {
                    Bootstrap = b,
                    AssemblyName = b.GetType().GetTypeInfo().Assembly.GetName().Name,
                })
                .ToList();
            var eventFlowBootstraps = list
                .Where(a => a.AssemblyName.StartsWith("EventFlow"))
                .OrderBy(a => a.AssemblyName)
                .Select(a => a.Bootstrap);
            var otherBootstraps = list
                .Where(a => !a.AssemblyName.StartsWith("EventFlow"))
                .OrderBy(a => a.AssemblyName)
                .Select(a => a.Bootstrap);
            return eventFlowBootstraps.Concat(otherBootstraps).ToList();
        }
    }
}
