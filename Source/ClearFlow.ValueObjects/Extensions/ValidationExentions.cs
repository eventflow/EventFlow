using ClearFlow.ValueObjects.ComplexValue;
using ClearFlow.ValueObjects.Dtos;
using ClearFlow.ValueObjects.SingleValue;
using FluentValidation;

namespace ClearFlow.ValueObjects.Extensions;

public static class ValidationExentions
{
    public static IRuleBuilderOptions<TModel, string> IsEmailValue<TModel>(this IRuleBuilder<TModel, string> ruleBuilder)
    {
        return EmailValue.Validate(ruleBuilder);
    }

    public static IRuleBuilderOptions<TModel, string> IsStringValue<TModel>(this IRuleBuilder<TModel, string> ruleBuilder)
    {
        return StringValue.Validate(ruleBuilder);
    }

    public static IRuleBuilderOptions<TModel, string> IsPersonNameValue<TModel>(this IRuleBuilder<TModel, string> ruleBuilder)
    {
        return PersonNameValue.Validate(ruleBuilder);
    }

    public static IRuleBuilderOptions<TModel, MoneyDto> IsMoneyValue<TModel>(this IRuleBuilder<TModel, MoneyDto> ruleBuilder)
    {
        return MoneyValue.Validate(ruleBuilder);
    }

    public static IRuleBuilderOptions<TModel, MobileNumberDto> IsMobileNumberValue<TModel>(this IRuleBuilder<TModel, MobileNumberDto> ruleBuilder)
    {
        return MobileNumberValue.Validate(ruleBuilder);
    }
}
