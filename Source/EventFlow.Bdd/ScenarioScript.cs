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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Bdd.Contexts;

namespace EventFlow.Bdd
{
    public class ScenarioScript : IScenarioScript
    {
        private readonly List<IScenarioStep> _givenSteps = new List<IScenarioStep>();
        private readonly List<IScenarioStep> _thenSteps = new List<IScenarioStep>();
        private readonly List<IScenarioStep> _whenSteps = new List<IScenarioStep>();

        public ScenarioState State { get; private set; }

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
            State = ScenarioState.Given;
            await ExecuteAsync(_givenSteps, cancellationToken).ConfigureAwait(false);

            // When
            State = ScenarioState.When;
            await ExecuteAsync(_whenSteps, cancellationToken).ConfigureAwait(false);

            // ThenContext
            State = ScenarioState.Then;
            await ExecuteAsync(_thenSteps, cancellationToken).ConfigureAwait(false);

            // End
            State = ScenarioState.Unknown;
        }

        public void Dispose()
        {
            foreach (var scenarioStep in _givenSteps.Concat(_whenSteps).Concat(_thenSteps))
            {
                scenarioStep.Dispose();
            }
        }

        private static async Task ExecuteAsync(IEnumerable<IScenarioStep> scenarioSteps, CancellationToken cancellationToken)
        {
            foreach (var scenarioStep in scenarioSteps)
            {
                await scenarioStep.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}