using System;
using System.Collections.Generic;
using EventFlow.ValueObjects;
using FluentValidation;
using FluentValidation.Results;

namespace ClearFlow.ValueObjects.Core;

public abstract class ValidatedSingleValueObject<TValue, TValidator> : SingleValueObject<TValue>
    where TValue : IComparable
    where TValidator : ValueObjectValidator<TValue>, new()
{
    private static TValidator validator = new TValidator();

    public ValidatedSingleValueObject(TValue value) : base(value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        validator.ValidateAndThrow(value);
    }

    public static IReadOnlyList<ValidationFailure> Validate(TValue value)
    {
        return validator.Validate(value).Errors;
    }

    public static IRuleBuilderOptions<TProp, TValue> Validate<TProp>(IRuleBuilder<TProp, TValue> builder)
    {
        return validator.ApplyRules(builder);
    }
}
