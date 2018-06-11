using System.Threading;
using EventFlow.Configuration.Bootstraps;
using EventFlow.Core;

namespace EventFlow.Extensions
{
    public static class BootstrapperExtensions
    {
        public static void Start(this IBootstrapper bootstrapper)
        {
            if (bootstrapper is Bootstrapper b && b.HasBeenRun)
            {
                return;
            }

            using (var a = AsyncHelper.Wait)
            {
                a.Run(bootstrapper.StartAsync(CancellationToken.None));
            }
        }
    }
}
