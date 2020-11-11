using EventFlow;
using EventFlow.Configuration;
using EventFlow.Extensions;

namespace EventFlow.WebApp.Template.Domain
{
    public class Module : IModule
    {
        public void Register(IEventFlowOptions options)
        {
            options.AddDefaults(typeof(Entity).Assembly);
        }
    }
}
