using EventFlow.Core;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ClearFlow.FluentValidation.Mvc.Filters;

public class EnumExampleValueSchemaFilter : ISchemaFilter
{
    private readonly IJsonSerializer serializer;

    public EnumExampleValueSchemaFilter(IJsonSerializer serializer)
    {
        this.serializer = serializer;
    }

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var declaringType = context.Type;
        if (declaringType != null && declaringType.IsEnum)
        {
            var enumValues = declaringType.GetEnumValues();
            var lastEnumValue = enumValues.GetValue(enumValues.Length - 1)?.ToString() ?? string.Empty;
            schema.Example = OpenApiAnyFactory.CreateFromJson(serializer.Serialize(lastEnumValue));
            return;
        }
    }
}
