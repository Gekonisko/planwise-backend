using PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Messaging;
using PlanWise.Modules.IdentityAccess.Application.Services;
using PlanWise.Modules.IdentityAccess.Domain.Abstractions;
using PlanWise.Modules.IdentityAccess.Domain.Users;

namespace PlanWise.Modules.IdentityAccess.Application.Users.Login;

internal sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    AuthenticationService authenticationService)
    : ICommandHandler<LoginCommand, AuthenticationResponse>
{
    public async Task<Result<AuthenticationResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetByEmailAsync(
            request.Email.Trim().ToLowerInvariant(),
            cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result.Failure<AuthenticationResponse>(UserErrors.InvalidCredentials());
        }

        return await authenticationService.CreateSessionAsync(user, cancellationToken);
    }
}