using PlanWise.Common.Domain;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Messaging;
using PlanWise.Modules.IdentityAccess.Application.Services;
using PlanWise.Modules.IdentityAccess.Domain.Users;
using PlanWise.Modules.IdentityAccess.Domain.Abstractions;

namespace PlanWise.Modules.IdentityAccess.Application.Users.Register;

internal sealed class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    AuthenticationService authenticationService)
    : ICommandHandler<RegisterUserCommand, AuthenticationResponse>
{
    public async Task<Result<AuthenticationResponse>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            return Result.Failure<AuthenticationResponse>(UserErrors.EmailNotUnique(request.Email));
        }

        var user = User.Create(
            request.Email.Trim().ToLowerInvariant(),
            request.FirstName.Trim(),
            request.LastName.Trim(),
            passwordHasher.Hash(request.Password));

        userRepository.Create(user);

        return await authenticationService.CreateSessionAsync(user, cancellationToken);
    }
}