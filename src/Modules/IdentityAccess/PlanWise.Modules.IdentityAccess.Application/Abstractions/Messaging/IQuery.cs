using MediatR;
using PlanWise.Common.Domain;

namespace PlanWise.Modules.IdentityAccess.Application.Abstractions.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
