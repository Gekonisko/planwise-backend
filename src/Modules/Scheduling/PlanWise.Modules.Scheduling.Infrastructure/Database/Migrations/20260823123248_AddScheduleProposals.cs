using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanWise.Modules.Scheduling.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddScheduleProposals : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "schedule_proposals",
            schema: "scheduling",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                project_id = table.Column<Guid>(type: "uuid", nullable: false),
                job_id = table.Column<Guid>(type: "uuid", nullable: false),
                model_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                objective = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                constraints_honoured = table.Column<string[]>(type: "text[]", nullable: false),
                constraints_relaxed = table.Column<string[]>(type: "text[]", nullable: false),
                expected_gain = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_schedule_proposals", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "proposed_assignment",
            schema: "scheduling",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                task_id = table.Column<Guid>(type: "uuid", nullable: false),
                task_key = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                current_assignee_id = table.Column<Guid>(type: "uuid", nullable: true),
                proposed_assignee_id = table.Column<Guid>(type: "uuid", nullable: false),
                proposed_assignee_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                is_applied = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_proposed_assignment", x => x.id);
                table.ForeignKey(
                    name: "fk_proposed_assignment_schedule_proposals_proposal_id",
                    column: x => x.proposal_id,
                    principalSchema: "scheduling",
                    principalTable: "schedule_proposals",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_proposed_assignment_proposal_id",
            schema: "scheduling",
            table: "proposed_assignment",
            column: "proposal_id");

        migrationBuilder.CreateIndex(
            name: "ix_schedule_proposals_project_id",
            schema: "scheduling",
            table: "schedule_proposals",
            column: "project_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "proposed_assignment",
            schema: "scheduling");

        migrationBuilder.DropTable(
            name: "schedule_proposals",
            schema: "scheduling");
    }
}
