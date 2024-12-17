using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json.Nodes;

namespace ClearFlow.FluentValidation.Mvc.Middleware;

public class RequestAggregateValueMapperMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IReadOnlyList<string> aggregateMappingKeys;
    private readonly string aggregatePropertyName;

    public RequestAggregateValueMapperMiddleware(RequestDelegate next, IReadOnlyList<string> aggregateMappingKeys, string aggregatePropertyName = "aggregateValue")
    {
        _next = next;

        this.aggregateMappingKeys = aggregateMappingKeys;
        this.aggregatePropertyName = aggregatePropertyName;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var nonPayloadMethods = context.Request.Method == HttpMethods.Get ||
            context.Request.Method == HttpMethods.Delete;

        var notJsonPayload = !context.Request.HasJsonContentType();
        if (nonPayloadMethods || notJsonPayload)
        {
            await _next(context);
            return;
        }

        var aggregateValue = context.Request.RouteValues.Where(x => aggregateMappingKeys.Any(mapperKey => x.Key.Equals(mapperKey, StringComparison.InvariantCultureIgnoreCase))).ToList();
        var hasNoAggregateValue = !aggregateValue.Any();
        if (hasNoAggregateValue)
        {
            await _next(context);
            return;
        }

        var valuePair = aggregateValue.First();

        context.Request.EnableBuffering();
        var bodyAsText = await new StreamReader(context.Request.Body).ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(bodyAsText))
        {
            bodyAsText = "{}";
        }

        var jsonPayload = JsonObject.Parse(bodyAsText)?.AsObject();
        if(jsonPayload == null)
        {
            await _next(context);
            return;
        }

        jsonPayload.Remove(aggregatePropertyName);
        jsonPayload.TryAdd(aggregatePropertyName, JsonValue.Create(valuePair.Value));
        var newBody = jsonPayload.ToJsonString();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(newBody));
        context.Request.ContentLength = context.Request.Body.Length;

        await _next(context);
    }
}