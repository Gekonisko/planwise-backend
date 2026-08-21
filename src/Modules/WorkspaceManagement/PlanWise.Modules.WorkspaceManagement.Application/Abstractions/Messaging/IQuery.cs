using MediatR;
using PlanWise.Common.Domain;

namespace PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;