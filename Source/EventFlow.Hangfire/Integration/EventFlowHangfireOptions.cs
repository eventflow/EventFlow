using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventFlow.Hangfire.Integration
{
    internal class EventFlowHangfireOptions : IEventFlowHangfireOptions
    {
        private readonly IEventFlowOptions _eventFlowOptions;

        public EventFlowHangfireOptions(IEventFlowOptions eventFlowOptions)
        {
            _eventFlowOptions = eventFlowOptions;
        }

        public IEventFlowHangfireOptions UseQueueName(string queueName)
        {
            _eventFlowOptions.RegisterServices(sr => sr.Register<IQueueNameProvider>(r => new QueueNameProvider(queueName)));
            return this;
        }
    }
}
