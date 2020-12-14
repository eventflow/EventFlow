// The MIT License (MIT)
// 
// Copyright (c) 2015-2020 Rasmus Mikkelsen
// Copyright (c) 2015-2020 eBay Software Foundation
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

namespace EventFlow.TestHelpers
{
    public static class ProcessHelper
    {
        public static IDisposable StartExe(
            string exePath,
            string initializationDone,
            params string[] arguments)
        {
            if (string.IsNullOrEmpty(exePath)) throw new ArgumentNullException(nameof(exePath));
            if (string.IsNullOrEmpty(initializationDone)) throw new ArgumentNullException(nameof(initializationDone));

            var workingDirectory = Path.GetDirectoryName(exePath);
            if (string.IsNullOrEmpty(workingDirectory)) throw new ArgumentException($"Could not find directory for '{exePath}'", nameof(exePath));

            LogHelper.Log.Information($"Starting process: '{exePath}' {string.Join(" ", arguments)}");

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
                    LogHelper.Log.Information($"OUT - {exeName}: {e.Data}");
                }
            }
            void ErrHandler(object sender, DataReceivedEventArgs e)
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    LogHelper.Log.Error($"ERR - {exeName}: {e.Data}");
                }
            }
            void InitializeProcess(Process p)
            {
                LogHelper.Log.Information($"{exeName} START =======================================");
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
#if NET452
                        KillProcessAndChildren(process.Id);
#endif
                    }
                    catch (Exception e)
                    {
                        LogHelper.Log.Error($"Failed to kill process: {e.Message}");
                    }
                    finally
                    {
                        process.DisposeSafe("Process");
                    }
                });
        }

#if NET452
        private static void KillProcessAndChildren(int pid)
        {
            var searcher = new System.Management.ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
            var moc = searcher.Get();

            foreach (var o in moc)
            {
                var mo = (System.Management.ManagementObject)o;
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }

            try
            {
                LogHelper.Log.Information($"Killing process {pid}");

                var proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }
#endif
    }
}