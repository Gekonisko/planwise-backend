using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;

namespace PlanWise.Common.Application.Jobs.GetJob;

public sealed record GetJobQuery(Guid JobId) : IQuery<AsyncJobResponse>;
