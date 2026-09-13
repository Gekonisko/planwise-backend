using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanWise.Modules.RiskPrediction.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddTaskFeatureSnapshots : Migration
{
    // Hoisted out of the CreateIndex call to satisfy CA1861 (no constant array arguments).
    private static readonly string[] UnresolvedIndexColumns = ["project_id", "outcome_status", "outcome_is_censored"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "task_feature_snapshots",
            schema: "risk_prediction",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                run_id = table.Column<Guid>(type: "uuid", nullable: false),
                project_id = table.Column<Guid>(type: "uuid", nullable: false),
                task_id = table.Column<Guid>(type: "uuid", nullable: false),
                task_key = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                captured_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                captured_on = table.Column<DateOnly>(type: "date", nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                points = table.Column<int>(type: "integer", nullable: true),
                business_value = table.Column<int>(type: "integer", nullable: true),
                has_due_date = table.Column<bool>(type: "boolean", nullable: false),
                days_until_due = table.Column<int>(type: "integer", nullable: true),
                due_date = table.Column<DateOnly>(type: "date", nullable: true),
                open_predecessor_count = table.Column<int>(type: "integer", nullable: false),
                total_predecessor_count = table.Column<int>(type: "integer", nullable: false),
                blocks_count = table.Column<int>(type: "integer", nullable: false),
                subtask_total = table.Column<int>(type: "integer", nullable: false),
                subtask_done = table.Column<int>(type: "integer", nullable: false),
                is_assigned = table.Column<bool>(type: "boolean", nullable: false),
                assignee_open_task_count = table.Column<int>(type: "integer", nullable: false),
                assignee_open_points = table.Column<int>(type: "integer", nullable: false),
                is_in_sprint = table.Column<bool>(type: "boolean", nullable: false),
                days_left_in_sprint = table.Column<int>(type: "integer", nullable: true),
                project_open_task_count = table.Column<int>(type: "integer", nullable: false),
                predicted_probability_of_slip = table.Column<decimal>(type: "numeric", nullable: false),
                predicted_day_impact = table.Column<int>(type: "integer", nullable: false),
                model_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                outcome_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                outcome_days_late = table.Column<int>(type: "integer", nullable: true),
                outcome_is_censored = table.Column<bool>(type: "boolean", nullable: false),
                outcome_resolved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                task_completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_task_feature_snapshots", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_task_feature_snapshots_project_id_outcome_status_outcome_is",
            schema: "risk_prediction",
            table: "task_feature_snapshots",
            columns: UnresolvedIndexColumns);

        migrationBuilder.CreateIndex(
            name: "ix_task_feature_snapshots_run_id",
            schema: "risk_prediction",
            table: "task_feature_snapshots",
            column: "run_id");

        migrationBuilder.CreateIndex(
            name: "ix_task_feature_snapshots_task_id",
            schema: "risk_prediction",
            table: "task_feature_snapshots",
            column: "task_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "task_feature_snapshots",
            schema: "risk_prediction");
    }
}
