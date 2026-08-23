using PlanWise.Common.Domain;

namespace PlanWise.Common.Application.Jobs;

public static class JobErrors
{
    public static Error NotFound(Guid jobId) =>
        Error.NotFound("Job.NotFound", $"The job with identifier {jobId} was not found");
}
