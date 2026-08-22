using FluentValidation;
using MediatR;
using PlanWise.Common.Application.Behaviors;
using PlanWise.Common.Domain;
using PlanWise.Modules.IdentityAccess.Application.Users.Login;

namespace PlanWise.Modules.IdentityAccess.Application.Tests;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Invalid_login_does_not_invoke_handler()
    {
        ValidationPipelineBehavior<LoginCommand, Result<object>> behavior = new(
            [new LoginCommandValidator()]);
        bool handlerCalled = false;

        Result<object> result = await behavior.Handle(
            new LoginCommand(string.Empty, string.Empty, false),
            _ =>
            {
                handlerCalled = true;
                return Task.FromResult(Result.Success<object>(new object()));
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.False(handlerCalled);
        Assert.IsType<ValidationError>(result.Error);
        Assert.Equal(3, ((ValidationError)result.Error).Errors.Length);
    }
}