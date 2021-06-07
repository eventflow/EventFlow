using Hangfire.Common;
using Hangfire.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventFlow.Hangfire.Integration
{
    internal class UseQueueFromParameterAttribute : JobFilterAttribute, IElectStateFilter
    {
        public UseQueueFromParameterAttribute(int parameterIndex)
        {
            if (parameterIndex < 0)
                throw new InvalidOperationException("Invalid queue name parameter index");

            ParameterIndex = parameterIndex;
        }

        public int ParameterIndex { get; }

        public void OnStateElection(ElectStateContext context)
        {
            var enqueuedState = context.CandidateState as EnqueuedState;
            if (enqueuedState != null)
            {
                if (ParameterIndex >= context.BackgroundJob.Job.Args.Count)
                    throw new InvalidOperationException("Invalid queue name parameter index");

                var queueName = context.BackgroundJob.Job.Args[ParameterIndex] as string;

                if (queueName != null)
                    enqueuedState.Queue = queueName;
            }
        }
    }
}
