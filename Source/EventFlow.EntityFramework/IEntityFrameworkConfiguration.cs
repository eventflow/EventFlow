using EventFlow.Configuration;

namespace EventFlow.EntityFramework
{
    public interface IEntityFrameworkConfiguration
    {
        string ConnectionString { get; }
        void Apply(IServiceRegistration serviceRegistration);
    }
}
