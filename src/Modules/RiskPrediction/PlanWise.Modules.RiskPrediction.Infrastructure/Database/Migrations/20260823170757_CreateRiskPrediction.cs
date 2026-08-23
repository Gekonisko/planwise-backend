using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanWise.Modules.RiskPrediction.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class CreateRiskPrediction : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "risk_prediction");

        migrationBuilder.CreateTable(
            name: "risk_assessment_runs",
            schema: "risk_prediction",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                project_id = table.Column<Guid>(type: "uuid", nullable: false),
                job_id = table.Column<Guid>(type: "uuid", nullable: false),
                model_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                training_window_days = table.Column<int>(type: "integer", nullable: false),
                assumptions = table.Column<string[]>(type: "text[]", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_risk_assessment_runs", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "sprint_forecasts",
            schema: "risk_prediction",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                run_id = table.Column<Guid>(type: "uuid", nullable: false),
                project_id = table.Column<Guid>(type: "uuid", nullable: false),
                sprint_id = table.Column<Guid>(type: "uuid", nullable: false),
                completion_probability = table.Column<decimal>(type: "numeric", nullable: false),
                expected_points = table.Column<decimal>(type: "numeric", nullable: false),
                p50delivery_date = table.Column<DateOnly>(type: "date", nullable: false),
                p90delivery_date = table.Column<DateOnly>(type: "date", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_sprint_forecasts", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "task_risk_assessments",
            schema: "risk_prediction",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                run_id = table.Column<Guid>(type: "uuid", nullable: false),
                project_id = table.Column<Guid>(type: "uuid", nullable: false),
                task_id = table.Column<Guid>(type: "uuid", nullable: false),
                task_key = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                probability_of_slip = table.Column<decimal>(type: "numeric", nullable: false),
                day_impact = table.Column<int>(type: "integer", nullable: false),
                reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                feature_contributions_json = table.Column<string>(type: "jsonb", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                dismissed = table.Column<bool>(type: "boolean", nullable: false),
                dismissed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                dismissed_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_task_risk_assessments", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_risk_assessment_runs_project_id",
            schema: "risk_prediction",
            table: "risk_assessment_runs",
            column: "project_id");

        migrationBuilder.CreateIndex(
            name: "ix_sprint_forecasts_project_id",
            schema: "risk_prediction",
            table: "sprint_forecasts",
            column: "project_id");

        migrationBuilder.CreateIndex(
            name: "ix_sprint_forecasts_run_id",
            schema: "risk_prediction",
            table: "sprint_forecasts",
            column: "run_id");

        migrationBuilder.CreateIndex(
            name: "ix_sprint_forecasts_sprint_id",
            schema: "risk_prediction",
            table: "sprint_forecasts",
            column: "sprint_id");

        migrationBuilder.CreateIndex(
            name: "ix_task_risk_assessments_project_id",
            schema: "risk_prediction",
            table: "task_risk_assessments",
            column: "project_id");

        migrationBuilder.CreateIndex(
            name: "ix_task_risk_assessments_run_id",
            schema: "risk_prediction",
            table: "task_risk_assessments",
            column: "run_id");

        migrationBuilder.CreateIndex(
            name: "ix_task_risk_assessments_task_id",
            schema: "risk_prediction",
            table: "task_risk_assessments",
            column: "task_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "risk_assessment_runs",
            schema: "risk_prediction");

        migrationBuilder.DropTable(
            name: "sprint_forecasts",
            schema: "risk_prediction");

        migrationBuilder.DropTable(
            name: "task_risk_assessments",
            schema: "risk_prediction");
    }
}
