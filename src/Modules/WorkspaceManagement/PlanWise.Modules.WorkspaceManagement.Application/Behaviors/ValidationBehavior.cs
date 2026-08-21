using FluentValidation;
using MediatR;
using PlanWise.Common.Domain;

namespace PlanWise.Modules.WorkspaceManagement.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, Result<TResponse>>
    where TRequest : notnull
{
    public async Task<Result<TResponse>> Handle(
        TRequest request,
        RequestHandlerDelegate<Result<TResponse>> next,
        CancellationToken cancellationToken)
    {
        FluentValidation.Results.ValidationFailure[] failures = (await Task.WhenAll(
                validators.Select(validator => validator.ValidateAsync(request, cancellationToken))))
            .SelectMany(result => result.Errors)
            .ToArray();

        if (failures.Length == 0)
        {
            return await next(cancellationToken);
        }

        return Result.Failure<TResponse>(new Error(
            "Validation.Invalid",
            string.Join("; ", failures.Select(failure => failure.ErrorMessage)),
            ErrorType.Validation));
    }
}