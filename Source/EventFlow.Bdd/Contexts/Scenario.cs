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
using EventFlow.Core;

namespace EventFlow.Bdd.Contexts
{
    public class Scenario : IScenario, IScenarioContext
    {
        private readonly IGivenContext _givenContext;
        private readonly IWhenContext _whenContext;
        private readonly IThenContext _thenContext;

        public Scenario(
            IGivenContext givenContext,
            IWhenContext whenContext,
            IThenContext thenContext,
            IScenarioScript scenarioScript)
        {
            _givenContext = givenContext;
            _whenContext = whenContext;
            _thenContext = thenContext;

            _givenContext.Setup(this);
            _whenContext.Setup(this);
            _thenContext.Setup(this);

            Script = scenarioScript;
        }

        public IScenarioScript Script { get; }

        public IScenario Given(Action<IGiven> action)
        {
            action(_givenContext);
            return this;
        }

        public IScenario When(Action<IWhen> action)
        {
            action(_whenContext);
            return this;
        }

        public IScenario Then(Action<IThen> action)
        {
            action(_thenContext);
            using (var a = AsyncHelper.Wait)
            {
                a.Run(Script.ExecuteAsync(CancellationToken.None));
            }
            return this;
        }

        public void Dispose()
        {
        }
    }
}