using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventFlow.Hangfire.Integration
{
    internal class QueueNameProvider : IQueueNameProvider
    {
        public string QueueName { get; }

        public QueueNameProvider(string queueName)
        {
            QueueName = queueName;
        }
    }
}
