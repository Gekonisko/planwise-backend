using PlanWise.Common.Domain;
using MediatR;

namespace PlanWise.Common.Application.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
