using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ClearFlow.FluentValidation.Mvc.Filters;

public class AggregateValueAutoAddedSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema == null)
        {
            return;
        }

        if (schema.ReadOnly == true && context.MemberInfo?.Name != null && context.MemberInfo.Name.Contains("aggregateValue", StringComparison.InvariantCultureIgnoreCase))
        {
            var checkParameters = context.MemberInfo.ReflectedType;
            var primaryConstructor = checkParameters?.GetConstructors()?.FirstOrDefault();
            if (primaryConstructor == null)
            {
                return;
            }

            var hasAggregateValue = primaryConstructor.GetParameters().Any(x => x.Name != null && x.Name.Equals("aggregateValue", StringComparison.InvariantCultureIgnoreCase));
            schema.ReadOnly = !hasAggregateValue;
        }
    }
}
