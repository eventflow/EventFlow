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
using EventFlow.Bdd.Contexts;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Bdd.Results;

namespace EventFlow.Bdd
{
    public class ScenarioRunner : IScenarioRunnerContext
    {
        private readonly List<IScenarioStep> _givenSteps = new List<IScenarioStep>();
        private readonly IGivenContext _givenContext;
        private readonly IWhenContext _whenContext;
        private readonly IThenContext _thenContext;
        private readonly List<IScenarioStep> _thenSteps = new List<IScenarioStep>();
        private readonly List<IScenarioStep> _whenSteps = new List<IScenarioStep>();

        public ScenarioRunner(
            IGivenContext givenContext,
            IWhenContext whenContext,
            IThenContext thenContext)
        {
            _givenContext = givenContext;
            _whenContext = whenContext;
            _thenContext = thenContext;

            _givenContext.Setup(this);
            _whenContext.Setup(this);
            _thenContext.Setup(this);
        }

        public ScenarioState State { get; private set; }
        public string Name { get; set; } = string.Empty;

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

        public async Task<ScriptResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            StateResult givenResult;
            StateResult whenResult = null;
            StateResult thenResult = null;

            // Given
            State = ScenarioState.Given;
            givenResult = await ExecuteAsync(_givenSteps, cancellationToken).ConfigureAwait(false);

            // When
            if (givenResult.Success)
            {
                State = ScenarioState.When;
                whenResult = await ExecuteAsync(_whenSteps, cancellationToken).ConfigureAwait(false);
            }

            // Then
            if (givenResult.Success && whenResult != null && whenResult.Success)
            {
                State = ScenarioState.Then;
                thenResult = await ExecuteAsync(_thenSteps, cancellationToken).ConfigureAwait(false);
            }

            // End
            State = ScenarioState.Unknown;
            return new ScriptResult(Name, givenResult, whenResult, thenResult);
        }

        public void Dispose()
        {
            foreach (var scenarioStep in _givenSteps.Concat(_whenSteps).Concat(_thenSteps))
            {
                scenarioStep.Dispose();
            }
        }

        private static async Task<StateResult> ExecuteAsync(IEnumerable<IScenarioStep> scenarioSteps, CancellationToken cancellationToken)
        {
            var stepResults = new List<StepResult>();

            foreach (var scenarioStep in scenarioSteps)
            {
                try
                {
                    var stepResult = await scenarioStep.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                    stepResults.Add(stepResult);
                }
                catch (Exception e)
                {
                    stepResults.Add(StepResult.Failed(scenarioStep.Name, e));
                    break;
                }
            }

            return new StateResult(stepResults);
        }

        public IScenarioRunner Given(Action<IGiven> action)
        {
            action(_givenContext);
            return this;
        }

        public IScenarioRunner When(Action<IWhen> action)
        {
            action(_whenContext);
            return this;
        }

        public IScenarioRunner Then(Action<IThen> action)
        {
            action(_thenContext);
            return this;
        }
    }
}