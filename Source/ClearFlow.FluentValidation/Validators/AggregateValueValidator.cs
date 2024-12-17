using EventFlow.Aggregates;
using EventFlow.Core;
using FluentValidation;
using EventFlow.Extensions;
using System.Text.RegularExpressions;
using EventFlow.Commands.Serialization;
using ClearFlow.FluentValidation.Commands;

namespace ClearFlow.FluentValidation.Validators;

public abstract class AggregateValueValidator<TCommand, TAggregate, TIdentity> : AbstractValidator<TCommand>
            where TCommand : SerializableCommand<TAggregate, TIdentity, ValidatedExecutionResult>
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : class, IIdentity
{
    private static readonly string Prefix = new Regex("Id$").Replace(typeof(TIdentity).Name, string.Empty).ToLowerInvariant() + "-";
    private static readonly Regex ValueValidation = new Regex(@"^[^\-]+\-(?<guid>[a-f0-9]{8}\-[a-f0-9]{4}\-[a-f0-9]{4}\-[a-f0-9]{4}\-[a-f0-9]{12})$", RegexOptions.Compiled);

    public AggregateValueValidator()
    {
        RuleFor(x => x.AggregateValue)
            .NotEmpty()
            .Matches($"^{Prefix}").WithMessage("Identity '{PropertyValue}'" + $"of type '{typeof(TIdentity).PrettyPrint()}' does not start with '{Prefix}'")
            .Matches(ValueValidation).WithMessage("Identity '{PropertyValue}'" + $"of type '{typeof(TIdentity).PrettyPrint()}' does not follow the syntax '{Prefix}[GUID]' in lower case");
    }
}