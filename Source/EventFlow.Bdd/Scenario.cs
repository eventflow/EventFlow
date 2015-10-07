// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen
// https://github.com/rasmus/EventFlow
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
using System.Threading;
using EventFlow.Bdd.Results;
using EventFlow.Bdd.Results.Formatters;
using EventFlow.Core;
using EventFlow.Logs;

namespace EventFlow.Bdd
{
    public class Scenario : IScenario
    {
        private string _name;
        private readonly ILog _log;
        private readonly Func<IScenarioRunnerContext> _scenarioScriptFactory;

        public Scenario(
            ILog log,
            Func<IScenarioRunnerContext> scenarioScriptFactory)
        {
            _log = log;
            _scenarioScriptFactory = scenarioScriptFactory;
        }

        public IScenario Named(string name)
        {
            _name = name;
            return this;
        }

        public IScenario Run(Action<IScenarioRunner> scenario)
        {
            using (var scenarioRunner = _scenarioScriptFactory())
            {
                scenarioRunner.Name = _name;
                scenario(scenarioRunner);

                ScriptResult scriptResult = null;
                using (var a = AsyncHelper.Wait)
                {
                    a.Run(scenarioRunner.ExecuteAsync(CancellationToken.None), r => scriptResult = r);
                }

                var result = new TextResultFormatter().Format(scriptResult);
                _log.Information(result);
            }

            return this;
        }

        public void Dispose()
        {
        }
    }
}