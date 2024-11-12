using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Commands;

namespace ClearFlow.FluentValidation.Commands;

public interface IValidatedCommandHandler<in TAggregate, TIdentity, in TCommand> : ICommandHandler<TAggregate, TIdentity, ValidatedExecutionResult, TCommand>
        where TAggregate : IAggregateRoot<TIdentity>
        where TIdentity : class, IIdentity
        where TCommand : ValidatedCommand<TCommand, TAggregate, TIdentity>
{

}

