// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
// https://github.com/eventflow/EventFlow
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Diagnostics;
using System.IO;
using EventFlow.Core;
using EventFlow.Extensions;
using EventFlow.TestHelpers.Extensions;
using Microsoft.Extensions.Logging;

namespace EventFlow.TestHelpers
{
    public static class ProcessHelper
    {
        public static IDisposable Start(
            string exePath,
            string initializationDone,
            params string[] arguments)
        {
            if (string.IsNullOrEmpty(exePath)) throw new ArgumentNullException(nameof(exePath));
            if (string.IsNullOrEmpty(initializationDone)) throw new ArgumentNullException(nameof(initializationDone));

            var workingDirectory = Path.GetDirectoryName(exePath);
            if (string.IsNullOrEmpty(workingDirectory)) throw new ArgumentException($"Could not find directory for '{exePath}'", nameof(exePath));

            LogHelper.Logger.LogInformation($"Starting process: '{exePath}' {string.Join(" ", arguments)}");

            var process = new Process
                {
                    StartInfo = new ProcessStartInfo(exePath, string.Join(" ", arguments))
                        {
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            RedirectStandardError = true,
                            RedirectStandardOutput = true,
                            WorkingDirectory = workingDirectory,
                        }
                };
            var exeName = Path.GetFileName(exePath);

            void OutHandler(object sender, DataReceivedEventArgs e)
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    LogHelper.Logger.LogInformation($"OUT - {exeName}: {e.Data}");
                }
            }
            void ErrHandler(object sender, DataReceivedEventArgs e)
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    LogHelper.Logger.LogInformation($"ERR - {exeName}: {e.Data}");
                }
            }
            void InitializeProcess(Process p)
            {
                LogHelper.Logger.LogInformation($"{exeName} START =======================================");
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
            }

            process.OutputDataReceived += OutHandler;
            process.ErrorDataReceived += ErrHandler;
            process.WaitForOutput(initializationDone, InitializeProcess);

            return new DisposableAction(() =>
                {
                    try
                    {
                        process.OutputDataReceived -= OutHandler;
                        process.ErrorDataReceived -= ErrHandler;

                        // TODO: Kill process and its children
                    }
                    catch (Exception e)
                    {
                        LogHelper.Logger.LogInformation($"Failed to kill process: {e.Message}");
                    }
                    finally
                    {
                        process.DisposeSafe(LogHelper.Logger, "Process");
                    }
                });
        }
    }
}
