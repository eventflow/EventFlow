using EventFlow.Attributes;
using EventFlow.Core;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;

namespace ClearFlow.FluentValidation.Mvc.Models;
public class ValidationApiErrorModel
{
    [StringDisplayValue("https://tools.ietf.org/html/rfc9110#section-15.5.1")]
    public string Type { get; }
    [StringDisplayValue("One or more validation errors occured.")]
    public string Title { get; }
    [IntDisplayValue(400)]
    public int Status { get; }
    public Dictionary<string, List<string>> Errors { get; }
    public string TraceId { get; }

    public ValidationApiErrorModel(string type, string title, int status, Dictionary<string, List<string>> errors, string traceId)
    {
        Type = type;
        Title = title;
        Status = status;
        Errors = errors;
        TraceId = traceId;
    }

    public static ValidationApiErrorModel From(IIdentity aggregateId, ISourceId sourceId, IReadOnlyCollection<ValidationFailure> failures)
    {
        var errors = failures.GroupBy(x => x.PropertyName).ToDictionary((keyValue) => keyValue.Key, (keyValue) => keyValue.Select(x => x.ErrorMessage).ToList());

        return new ValidationApiErrorModel("https://tools.ietf.org/html/rfc9110#section-15.5.1", "One or more validation errors occured.", StatusCodes.Status400BadRequest, errors, sourceId.Value);
    }
}