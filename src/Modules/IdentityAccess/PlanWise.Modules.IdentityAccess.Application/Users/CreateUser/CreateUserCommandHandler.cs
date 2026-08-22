using PlanWise.Common.Domain;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Data;
using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.IdentityAccess.Domain.Users;
using PlanWise.Modules.IdentityAccess.Domain.Abstractions;

namespace PlanWise.Modules.IdentityAccess.Application.Users.CreateUser;

internal sealed class CreateUserCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher)
    : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            return Result.Failure<Guid>(UserErrors.EmailNotUnique(request.Email));
        }

        var newUser = User.Create(
            request.Email,
            request.FirstName,
            request.LastName,
            passwordHasher.Hash(request.Password));

        userRepository.Create(newUser);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return newUser.Id;
    }
}
