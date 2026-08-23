namespace PlanWise.Modules.Delivery.Application.Workload;

public sealed record MemberWorkloadResponse(Guid? UserId, string Email, decimal Capacity, int AssignedPoints);

public sealed record WorkloadResponse(IReadOnlyList<MemberWorkloadResponse> Members);
