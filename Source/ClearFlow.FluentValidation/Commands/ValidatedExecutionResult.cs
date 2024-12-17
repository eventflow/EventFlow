using EventFlow.Aggregates.ExecutionResults;
using FluentValidation.Results;

namespace ClearFlow.FluentValidation.Commands;

public class ValidatedExecutionResult : ExecutionResult
{
    protected static string GenerateMessage(bool isSuccess, IEnumerable<ValidationFailure> errors) => isSuccess ? "Sucessful execution" :
        $"Failed execution due to: {string.Join(", ", errors.Select(x => x.ErrorMessage).ToList())}";

    public string Message { get; }
    public IReadOnlyCollection<ValidationFailure> Failures { get; }
    public override bool IsSuccess { get; }

    public ValidatedExecutionResult(bool isSuccess, IEnumerable<ValidationFailure> errors, string message = "")
    {
        IsSuccess = isSuccess;
        Message = GenerateMessage(isSuccess, errors);
        Failures = errors.ToList();
    }

    public static ValidatedExecutionResult Failed(IEnumerable<ValidationFailure> errors, string message)
    {
        return new ValidatedExecutionResult(false, errors, message);
    }

    public static ValidatedExecutionResult Failed(IEnumerable<ValidationFailure> errors)
    {
        return new ValidatedExecutionResult(false, errors);
    }

    public static ValidatedExecutionResult Failed(ValidationFailure error)
    {
        return Failed(new List<ValidationFailure>() { error });
    }

    public static new ValidatedExecutionResult Success()
    {
        return new ValidatedExecutionResult(true, Enumerable.Empty<ValidationFailure>());
    }

    public override string ToString()
    {
        return Message;
    }
}

public class ValidatedExecutionResult<TModel> : ValidatedExecutionResult
{
    public TModel Model { get; }

    public ValidatedExecutionResult(TModel model, bool isSuccess, IEnumerable<ValidationFailure> errors, string message = "")
        : base(isSuccess, errors, message)
    {
        Model = model;
    }

    public static ValidatedExecutionResult<TModel> Success(TModel model)
    {
        return new ValidatedExecutionResult<TModel>(model, true, Enumerable.Empty<ValidationFailure>());
    }

    public static ValidatedExecutionResult<TModel> Failed(TModel model, IEnumerable<ValidationFailure> errors)
    {
        return new ValidatedExecutionResult<TModel>(model, false, errors);
    }
}

