using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;

namespace PlanWise.Modules.IdentityAccess.Application.Users.Register;

public sealed record RegisterUserCommand(
    string Email,
    string FirstName,
    string LastName,
    string Password) : ICommand<AuthenticationResponse>;