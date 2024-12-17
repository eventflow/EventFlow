using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands.Serialization;
using EventFlow.Core;
using FluentValidation;
using EventFlow.Extensions;
using EventFlow;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ClearFlow.FluentValidation.Commands;

public abstract class ValidatedCommand<TCommand, TAggregate, TIdentity> : SerializableCommand<TAggregate, TIdentity, ValidatedExecutionResult>
            where TCommand : ValidatedCommand<TCommand, TAggregate, TIdentity>
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : class, IIdentity
{
    [JsonIgnore]
    protected abstract AbstractValidator<TCommand> validator { get; }

    [JsonIgnore]
    [ValidateNever]
    protected new ISourceId SourceId => base.SourceId;

    [JsonIgnore]
    [ValidateNever]
    protected new TIdentity AggregateId => base.AggregateId;

    protected ValidatedCommand(string aggregateValue)
        : base(aggregateValue)
    { }


    public new async Task<IExecutionResult> PublishAsync(ICommandBus commandBus, CancellationToken cancellationToken)
    {
        var currentType = this as TCommand;
        if (currentType == null)
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
