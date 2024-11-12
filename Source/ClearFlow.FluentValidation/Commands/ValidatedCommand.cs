using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands.Serialization;
using EventFlow.Core;
using FluentValidation;
using System.Text;
using EventFlow.Extensions;
using EventFlow;
using System.Data;

namespace ClearFlow.FluentValidation.Commands;

public abstract class ValidatedCommand<TCommand, TAggregate, TIdentity> : SerializableCommand<TAggregate, TIdentity, ValidatedExecutionResult>
            where TCommand : ValidatedCommand<TCommand, TAggregate, TIdentity>
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : class, IIdentity
{

    protected ValidatedCommand(string aggregateValue)
        : base(aggregateValue)
    {
    }

    protected abstract AbstractValidator<TCommand> GetAssociatedValidator();
    protected static byte[] GetTypeBytes() => typeof(TCommand).FullName.GetBytes();
    
    public new async Task<IExecutionResult> PublishAsync(ICommandBus commandBus, CancellationToken cancellationToken)
    {
        var validator = GetAssociatedValidator();
        var currentType = this as TCommand;
        if(currentType == null)
        {
            throw new InvalidOperationException(string.Format(
                    "Assigned validator '{0}' is not the same as the commands model '{1}'",
                    typeof(TCommand).PrettyPrint(),
                    this.GetType().PrettyPrint()));
        }

        var results = await validator.ValidateAsync(currentType, cancellationToken);
        if (!results.IsValid)
        {
            return ValidatedExecutionResult.Failed(results.Errors);
        }

        return await base.PublishAsync(commandBus, cancellationToken);
    }
}

