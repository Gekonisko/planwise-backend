using PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Data;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Messaging;
using PlanWise.Modules.IdentityAccess.Domain.Abstractions;
using PlanWise.Modules.IdentityAccess.Domain.Users;

namespace PlanWise.Modules.IdentityAccess.Application.Users.ResetPassword;

internal sealed class ResetPasswordCommandHandler(
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    ITokenService tokenService)
    : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        PasswordResetToken? resetToken = await passwordResetTokenRepository.GetAsync(
            tokenService.HashToken(request.Token),
            cancellationToken);

        if (resetToken is null || !resetToken.IsActive)
        {
            return Result.Failure(UserErrors.InvalidPasswordResetToken());
        }

        User? user = await userRepository.GetAsync(resetToken.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.InvalidPasswordResetToken());
        }

        user.ChangePassword(passwordHasher.Hash(request.Password));
        resetToken.MarkUsed(DateTime.UtcNow);
        await refreshTokenRepository.RevokeAllAsync(user.Id, DateTime.UtcNow, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}