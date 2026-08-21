using MediatR;
using PlanWise.Common.Domain;

namespace PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Messaging;

public interface ICommand : IRequest<Result>, IBaseCommand;
public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IBaseCommand;
public interface IBaseCommand;