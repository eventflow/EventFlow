using System;
using System.Diagnostics;
using System.Threading;

namespace EventFlow.EventStores.EventStore.Tests.Extensions
{
    public static class ProcessExtensions
    {
        public static bool WaitForOutput(
            this Process process,
            string output,
            Action<Process> initialize)
        {
            var autoResetEvent = new AutoResetEvent(false);
            DataReceivedEventHandler handler = (sender, args) =>
                {
                    if (args.Data.Contains(output))
                    {
                        autoResetEvent.Set();
                    }
                };
            process.OutputDataReceived += handler;
            initialize(process);
            var foundOutput = autoResetEvent.WaitOne(TimeSpan.FromSeconds(30));
            process.OutputDataReceived -= handler;
            return foundOutput;
        }
    }
}