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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EventFlow.Bdd
{
    public class ScenarioScript : IScenarioScript
    {
        private readonly List<IScenarioStep> _givenSteps = new List<IScenarioStep>();
        private readonly List<IScenarioStep> _whenSteps = new List<IScenarioStep>();
        private readonly List<IScenarioStep> _thenSteps = new List<IScenarioStep>();

        public void AddGiven(IScenarioStep scenarioStep)
        {
            _givenSteps.Add(scenarioStep);
        }

        public void AddWhen(IScenarioStep scenarioStep)
        {
            _whenSteps.Add(scenarioStep);
        }

        public void AddThen(IScenarioStep scenarioStep)
        {
            _thenSteps.Add(scenarioStep);
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            // Given
            await ExecuteAsync(_givenSteps, cancellationToken).ConfigureAwait(false);

            // When
            await ExecuteAsync(_whenSteps, cancellationToken).ConfigureAwait(false);

            // ThenContext
            await ExecuteAsync(_thenSteps, cancellationToken).ConfigureAwait(false);
        }

        private static async Task ExecuteAsync(IEnumerable<IScenarioStep> scenarioSteps, CancellationToken cancellationToken)
        {
            foreach (var scenarioStep in scenarioSteps)
            {
                await scenarioStep.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
        }
    }
}