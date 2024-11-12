using ClearFlow.FluentValidation.Mvc.Attributes;
using EventFlow.Aggregates;
using EventFlow.Attributes;
using EventFlow.Core;
using EventFlow.Extensions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace ClearFlow.FluentValidation.Mvc.Filters;

public class ExampleValueSchemaFilter : ISchemaFilter
{
    private readonly IJsonSerializer serializer;

    public ExampleValueSchemaFilter(IJsonSerializer serializer)
    {
        this.serializer = serializer;
    }

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var declaringType = context.Type;
        if (declaringType != null && declaringType.IsEnum)
        {
            var testType = declaringType.GetEnumValues();
            var testValue = testType.GetValue(testType.Length - 1)?.ToString() ?? string.Empty;
            schema.Example = OpenApiAnyFactory.CreateFromJson(serializer.Serialize(testValue));
        }

        var schemaFilterAttribute = context.MemberInfo?.GetCustomAttributes() ?? new List<Attribute>();
        var exampleValues = schemaFilterAttribute.Where(filter => filter.GetType().IsAssignableTo(typeof(IExampleDisplayValue))).ToList();
        var hasValues = exampleValues.Any();
        if (hasValues)
        {
            var attribute = exampleValues.First() as IExampleDisplayValue;
            schema.Example = OpenApiAnyFactory.CreateFromJson(serializer.Serialize(attribute!.Example));
        }

        var aggregateDescription = context.MemberInfo?.GetCustomAttributes<AggregateValueDescriptionAttribute>()
            .FirstOrDefault();
        if (aggregateDescription != null)
        {
            var baseType = context.MemberInfo?.ReflectedType?.BaseType; // By convention this is how we implement commands via EventFlow
            if (baseType != null)
            {
                if (!SetExample(baseType, schema))
                {
                    var baseTypeInner = baseType.BaseType;
                    if (baseTypeInner != null) // Lets check one level deeper as well (inheritance)
                    {
                        SetExample(baseTypeInner, schema);
                    }
                }
            }
        }
    }

    private bool SetExample(Type type, OpenApiSchema schema)
    {
        var aggregate = type.GetGenericArguments().FirstOrDefault(p => p.GetInterfaces().Any(q => q == typeof(IAggregateRoot)));
        if (aggregate != null)
        {
            var aggregateName = aggregate.GetAggregateName().Value.ToLower();
            schema.Example = OpenApiAnyFactory.CreateFromJson(serializer.Serialize($"{aggregateName}-{Guid.NewGuid()}"));
            return true;
        }

        return false;
    }
}
