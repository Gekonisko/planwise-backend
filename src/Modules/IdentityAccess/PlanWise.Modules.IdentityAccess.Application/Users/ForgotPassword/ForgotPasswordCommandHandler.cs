using PlanWise.Common.Domain;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Data;
using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.IdentityAccess.Domain.Users;

namespace PlanWise.Modules.IdentityAccess.Application.Users.ForgotPassword;

internal sealed class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IUnitOfWork unitOfWork,
    ITokenService tokenService,
    IPasswordResetSender passwordResetSender)
    : ICommandHandler<ForgotPasswordCommand>
{
    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetByEmailAsync(
            request.Email.Trim().ToLowerInvariant(),
            cancellationToken);

        if (user is null)
        {
            return Result.Success();
        }

        RefreshTokenData resetToken = tokenService.CreatePasswordResetToken();

        passwordResetTokenRepository.Add(PasswordResetToken.Create(
            user.Id,
            tokenService.HashToken(resetToken.Value),
            resetToken.ExpiresAtUtc));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await passwordResetSender.SendAsync(user.Email, resetToken.Value, cancellationToken);

        return Result.Success();
    }
}