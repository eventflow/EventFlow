using EventFlow.Aggregates;
using EventFlow.Core;
using FluentValidation;
using EventFlow.Extensions;
using EventFlow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace ClearFlow.FluentValidation.Commands;

public class ValidatedCommandBus : CommandBus, IValidatedCommandBus
{
    public ValidatedCommandBus(ILogger<CommandBus> logger, IServiceProvider serviceProvider, IAggregateStore aggregateStore, IMemoryCache memoryCache) 
        : base(logger, serviceProvider, aggregateStore, memoryCache)
    {
    }

    public async Task<ValidatedExecutionResult> PublishWithValidationAsync<TCommand, TAggregate, TIdentity>(ValidatedCommand<TCommand, TAggregate, TIdentity> command, CancellationToken cancellationToken)
        where TCommand : ValidatedCommand<TCommand, TAggregate, TIdentity>
        where TAggregate : IAggregateRoot<TIdentity>
        where TIdentity : class, IIdentity
    {
        var validator = _serviceProvider.GetService<IValidator<TCommand>>();
        if (validator == null)
        {
            return await base.PublishAsync(command, cancellationToken);
        }

        var currentType = command as TCommand;
        if (currentType == null)
        {
            throw new InvalidOperationException(string.Format(
                    "Assigned validator '{0}' is not the same as the commands model '{1}'",
                    typeof(TCommand).PrettyPrint(),
                    this.GetType().PrettyPrint()));
        }

        var results = await validator.ValidateAsync(currentType, cancellationToken);
        var failedWithErrors = !results.IsValid;
        if (failedWithErrors)
        {
            return ValidatedExecutionResult.Failed(results.Errors);
        }

        return await base.PublishAsync(command, cancellationToken);
    }
}

