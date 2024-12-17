using EventFlow.Attributes;
using EventFlow.Core;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text.RegularExpressions;

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
        if (schema == null)
        {
            return;
        }

        var source = (context.MemberInfo?.GetCustomAttributes() ?? new List<Attribute>())
            .Where((filter) => filter.GetType().IsAssignableTo(typeof(IDisplayValue))).ToList();
        if (!source.Any())
        {
            return;
        }

        var isAggregateValue = source.First() is AggregateIdDisplayValueAttribute;
        if (isAggregateValue)
        {
            var identityType = context.MemberInfo?.DeclaringType?.GetGenericArguments().FirstOrDefault(arguments => arguments.IsAssignableTo(typeof(IIdentity)));
            if (identityType != null)
            {
                var identityName = new Regex("Id$").Replace(identityType.Name, string.Empty).ToLowerInvariant();
                var example = $"{identityName}-{GuidFactories.Comb.CreateForString()}";
                schema.Example = OpenApiAnyFactory.CreateFromJson(serializer.Serialize(example));
                return;
            }
        }

        var attribute = source.First() as IDisplayValue;
        schema.Example = OpenApiAnyFactory.CreateFromJson(serializer.Serialize(attribute?.Example));
        return;
    }
}
