using FluentValidation;
using MediatR;
using PlanWise.Common.Domain;

namespace PlanWise.Modules.IdentityAccess.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, Result<TResponse>>
    where TRequest : notnull
{
    public async Task<Result<TResponse>> Handle(
        TRequest request,
        RequestHandlerDelegate<Result<TResponse>> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next(cancellationToken);
        }

        ValidationContext<TRequest> context = new(request);

        FluentValidation.Results.ValidationFailure[] validationFailures =
            (await Task.WhenAll(validators.Select(validator => validator.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToArray();

        if (validationFailures.Length == 0)
        {
            return await next(cancellationToken);
        }

        Error[] errors = validationFailures
            .Select(failure => new Error(
                failure.PropertyName,
                failure.ErrorMessage,
                ErrorType.Validation))
            .ToArray();

        return Result<TResponse>.ValidationFailure(new ValidationError(errors));
    }
}