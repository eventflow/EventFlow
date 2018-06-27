using EventFlow.Configuration;

namespace EventFlow.EntityFramework
{
    public interface IEntityFrameworkConfiguration
    {
        void Apply(IServiceRegistration serviceRegistration);
    }
}
