using FluentValidation;

namespace PlanWise.Modules.Delivery.Application.Tasks.Comments;

internal sealed class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(command => command.Body).NotEmpty().MaximumLength(5000);
    }
}
