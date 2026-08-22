using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.IdentityAccess.Application.Users.CreateUser;
public sealed record CreateUserCommand(
    string Email,
    string FirstName,
    string LastName,
    string Password) : ICommand<Guid>;
