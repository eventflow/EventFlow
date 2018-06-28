using EventFlow.Configuration;

namespace EventFlow.EntityFramework
{
    public interface IEntityFrameworkConfiguration
    {
        int ReadModelDeletionBatchSize { get; }
        void Apply(IServiceRegistration serviceRegistration);
    }
}
