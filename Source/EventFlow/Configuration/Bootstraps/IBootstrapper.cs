using System.Threading;
using System.Threading.Tasks;

namespace EventFlow.Configuration.Bootstraps
{
    public interface IBootstrapper
    {
        Task StartAsync(CancellationToken cancellationToken);
    }
}
