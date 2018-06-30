using EventFlow.Configuration;

namespace EventFlow.EntityFramework
{
    public interface IEntityFrameworkConfiguration
    {
        int BulkDeletionBatchSize { get; }
        void Apply(IServiceRegistration serviceRegistration);
    }
}
