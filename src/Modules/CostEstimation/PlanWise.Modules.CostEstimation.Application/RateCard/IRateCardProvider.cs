using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.CostEstimation.Application.Abstractions;

namespace PlanWise.Modules.CostEstimation.Application.RateCard;

// The rate card is per-project and derived from the project's own members: their role is what they
// do here and their hourly rate is what the project pays them. A fixed global reference table
// produced estimates priced for roles the project doesn't staff, which made the numbers arbitrary.
public interface IRateCardProvider
{
    Task<ProjectRateCard> GetRatesAsync(Guid projectId, CancellationToken cancellationToken = default);
}

/// <param name="Rates">One line per distinct role actually staffed on the project.</param>
/// <param name="IsFromProjectTeam">
/// False when the project has no members priced yet and the fallback reference table was used —
/// the caller surfaces this so an estimate built on placeholder rates is never mistaken for one
/// built on the real team.
/// </param>
/// <param name="UnpricedMembers">
/// Members excluded from every rate line because they have no hourly rate (or no role) set. Returned
/// so the UI can name exactly who is missing from the estimate instead of silently dropping them.
/// </param>
public sealed record ProjectRateCard(
    IReadOnlyList<RoleRate> Rates,
    bool IsFromProjectTeam,
    IReadOnlyList<RateCardMember> UnpricedMembers);

public sealed class ProjectMemberRateCardProvider(IProjectMembersService projectMembersService) : IRateCardProvider
{
    private const string Currency = "USD";

    // Only used when the project has nobody priced yet, so the model has *some* anchor rather than
    // inventing rates. Deliberately a single generic line: a longer table would reintroduce the
    // "roles that aren't on this project" problem this class exists to solve.
    private static readonly RoleRate[] FallbackRates = [new RoleRate("Team member", 75m, 1m, Currency, [])];

    public async Task<ProjectRateCard> GetRatesAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ProjectMemberSummary> members = await projectMembersService.GetMembersAsync(projectId, cancellationToken);

        // A member with no rate set yet would drag a role's blended rate towards zero, so they're
        // excluded from pricing (the role still appears as long as one member in it is priced).
        var priced = members
            .Where(member => member.HourlyRate > 0m && !string.IsNullOrWhiteSpace(member.Role))
            .ToList();

        var unpriced = members
            .Where(member => member.HourlyRate <= 0m || string.IsNullOrWhiteSpace(member.Role))
            .Select(ToRateCardMember)
            .OrderBy(member => member.Email, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (priced.Count == 0)
        {
            return new ProjectRateCard(FallbackRates, IsFromProjectTeam: false, unpriced);
        }

        var rates = priced
            // Roles are free text, so "Developer" and "developer" are the same role here.
            .GroupBy(member => member.Role.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new RoleRate(
                group.First().Role.Trim(),
                // Capacity-weighted: a half-time senior shouldn't move the blended rate as much as
                // a full-time one. Falls back to a plain mean if the whole role sits at 0 capacity.
                BlendedRate(group),
                group.Sum(member => member.Capacity),
                Currency,
                group
                    .Select(ToRateCardMember)
                    .OrderByDescending(member => member.Capacity)
                    .ThenBy(member => member.Email, StringComparer.OrdinalIgnoreCase)
                    .ToList()))
            .OrderByDescending(rate => rate.Headcount)
            .ThenBy(rate => rate.Role, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ProjectRateCard(rates, IsFromProjectTeam: true, unpriced);
    }

    private static RateCardMember ToRateCardMember(ProjectMemberSummary member) =>
        new(member.Email, member.Role?.Trim() ?? string.Empty, member.Capacity, member.HourlyRate);

    private static decimal BlendedRate(IEnumerable<ProjectMemberSummary> role)
    {
        var members = role.ToList();
        decimal capacity = members.Sum(member => member.Capacity);

        decimal blended = capacity > 0m
            ? members.Sum(member => member.HourlyRate * member.Capacity) / capacity
            : members.Average(member => member.HourlyRate);

        return Math.Round(blended, 2);
    }
}
