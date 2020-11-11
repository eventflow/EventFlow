using EventFlow.Configuration;
using EventFlow.Extensions;

namespace EventFlow.Library.Template
{
    public class Module : IModule
    {
        public void Register(IEventFlowOptions options)
        {
            options.AddDefaults(typeof(Entity).Assembly);
        }
    }
}
