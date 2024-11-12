using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Swashbuckle.AspNetCore.Annotations;

namespace ClearFlow.FluentValidation.Mvc.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class CommandIgnoreValueAttribute : SwaggerSchemaAttribute, IPropertyValidationFilter
{
    public CommandIgnoreValueAttribute()
    {
        ReadOnly = true;
    }

    public bool ShouldValidateEntry(ValidationEntry entry, ValidationEntry parentEntry)
    {
        return false;
    }
}
