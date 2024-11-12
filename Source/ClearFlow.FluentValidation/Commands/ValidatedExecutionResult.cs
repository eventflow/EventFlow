using EventFlow.Aggregates.ExecutionResults;
using FluentValidation.Results;

namespace ClearFlow.FluentValidation.Commands;

public class ValidatedExecutionResult : ExecutionResult
{
    public IReadOnlyCollection<ValidationFailure> Failures { get; }

    public ValidatedExecutionResult(IEnumerable<ValidationFailure> errors)
    {
        Failures = (errors ?? Enumerable.Empty<ValidationFailure>()).ToList();
    }

    public static ValidatedExecutionResult Failed(IEnumerable<ValidationFailure> errors)
    {
        return new ValidatedExecutionResult(errors);
    }
    public static ValidatedExecutionResult Successful()
    {
        return new ValidatedExecutionResult(Enumerable.Empty<ValidationFailure>());
    }

    public override bool IsSuccess => !Failures.Any();

    public override string ToString()
    {
        return Failures.Any()
            ? $"Failed execution due to: {string.Join(", ", Failures.Select(x => x.ErrorMessage).ToList())}"
            : "Successful execution";
    }
}

