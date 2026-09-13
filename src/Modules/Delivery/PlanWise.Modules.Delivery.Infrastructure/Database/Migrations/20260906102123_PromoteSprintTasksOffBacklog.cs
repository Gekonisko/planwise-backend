using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanWise.Modules.Delivery.Infrastructure.Database.Migrations;

/// <summary>
/// Data-only migration. <c>ProjectTask.ApplySprint</c> now keeps sprint membership and board status
/// in step, but rows written before that existed can hold a sprint while still sitting at
/// <c>Backlog</c> — and the board only renders the three post-backlog columns, so those tasks appeared
/// nowhere at all once a sprint was selected. This brings existing data up to the invariant.
/// </summary>
public partial class PromoteSprintTasksOffBacklog : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(
            """
            UPDATE delivery.tasks
            SET status = 'Todo'
            WHERE sprint_id IS NOT NULL
              AND status = 'Backlog';
            """);

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Deliberately not reversed: the pre-migration state is indistinguishable from a task the
        // user legitimately dragged into the first board column, so demoting every sprint-assigned
        // task there on rollback would destroy real board state rather than restore it.
    }
}
