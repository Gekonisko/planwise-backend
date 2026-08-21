using MediatR;
using PlanWise.Common.Domain;

namespace PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Messaging;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;