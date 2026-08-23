using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanWise.Modules.Scheduling.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class CreateScheduling : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "scheduling");

        migrationBuilder.CreateTable(
            name: "milestones",
            schema: "scheduling",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                project_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                due_date = table.Column<DateOnly>(type: "date", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_milestones", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "schedule_items",
            schema: "scheduling",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                project_id = table.Column<Guid>(type: "uuid", nullable: false),
                task_id = table.Column<Guid>(type: "uuid", nullable: false),
                start_date = table.Column<DateOnly>(type: "date", nullable: false),
                end_date = table.Column<DateOnly>(type: "date", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_schedule_items", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_milestones_project_id",
            schema: "scheduling",
            table: "milestones",
            column: "project_id");

        migrationBuilder.CreateIndex(
            name: "ix_schedule_items_project_id",
            schema: "scheduling",
            table: "schedule_items",
            column: "project_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "milestones",
            schema: "scheduling");

        migrationBuilder.DropTable(
            name: "schedule_items",
            schema: "scheduling");
    }
}
