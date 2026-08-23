using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanWise.Modules.BacklogPrioritisation.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class CreateBacklogPrioritisation : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "backlog_prioritisation");

        migrationBuilder.CreateTable(
            name: "priority_runs",
            schema: "backlog_prioritisation",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                project_id = table.Column<Guid>(type: "uuid", nullable: false),
                job_id = table.Column<Guid>(type: "uuid", nullable: false),
                model_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                dismissed_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                dismissed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                applied_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_priority_runs", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "priority_item",
            schema: "backlog_prioritisation",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                run_id = table.Column<Guid>(type: "uuid", nullable: false),
                task_id = table.Column<Guid>(type: "uuid", nullable: false),
                task_key = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                current_position = table.Column<int>(type: "integer", nullable: false),
                proposed_position = table.Column<int>(type: "integer", nullable: false),
                value_score = table.Column<decimal>(type: "numeric", nullable: false),
                dependency_score = table.Column<decimal>(type: "numeric", nullable: false),
                complexity_score = table.Column<decimal>(type: "numeric", nullable: false),
                risk_score = table.Column<decimal>(type: "numeric", nullable: false),
                reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_priority_item", x => x.id);
                table.ForeignKey(
                    name: "fk_priority_item_priority_runs_run_id",
                    column: x => x.run_id,
                    principalSchema: "backlog_prioritisation",
                    principalTable: "priority_runs",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_priority_item_run_id",
            schema: "backlog_prioritisation",
            table: "priority_item",
            column: "run_id");

        migrationBuilder.CreateIndex(
            name: "ix_priority_runs_project_id",
            schema: "backlog_prioritisation",
            table: "priority_runs",
            column: "project_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "priority_item",
            schema: "backlog_prioritisation");

        migrationBuilder.DropTable(
            name: "priority_runs",
            schema: "backlog_prioritisation");
    }
}
