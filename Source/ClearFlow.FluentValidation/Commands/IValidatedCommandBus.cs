using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow;

namespace ClearFlow.FluentValidation.Commands;

public interface IValidatedCommandBus : ICommandBus
{
    Task<ValidatedExecutionResult> PublishWithValidationAsync<TCommand, TAggregate, TIdentity>(ValidatedCommand<TCommand, TAggregate, TIdentity> command, CancellationToken cancellationToken)
        where TCommand : ValidatedCommand<TCommand, TAggregate, TIdentity>
        where TAggregate : IAggregateRoot<TIdentity>
        where TIdentity : class, IIdentity;

}

