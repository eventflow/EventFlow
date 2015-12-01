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

using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Bdd.Results;
using EventFlow.Commands;
using EventFlow.Configuration;
using EventFlow.Core;

namespace EventFlow.Bdd.Steps.WhenSteps
{
    public class CommandScenarioStep<TAggregate, TIdentity, TSourceIdentity> : IScenarioStep
        where TAggregate : IAggregateRoot<TIdentity>
        where TIdentity : IIdentity
        where TSourceIdentity : ISourceId
    {
        private readonly IResolver _resolver;
        private readonly ICommand<TAggregate, TIdentity, TSourceIdentity> _command;

        public string Name { get; }

        public CommandScenarioStep(
            IResolver resolver,
            ICommand<TAggregate, TIdentity, TSourceIdentity> command)
        {
            _resolver = resolver;
            _command = command;

            var commandDefinition = _resolver.Resolve<ICommandDefinitionService>().GetDefinition(command.GetType());

            Name = $"{commandDefinition.Name} v{commandDefinition.Version} is published";
        }

        public async Task<StepResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            var commandBus = _resolver.Resolve<ICommandBus>();
            await commandBus.PublishAsync(_command, cancellationToken);
            return StepResult.Success(Name);
        }

        public void Dispose()
        {
        }
    }
}