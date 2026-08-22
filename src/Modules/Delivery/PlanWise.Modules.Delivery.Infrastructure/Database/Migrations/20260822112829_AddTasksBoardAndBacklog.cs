using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanWise.Modules.Delivery.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddTasksBoardAndBacklog : Migration
{
    private static readonly string[] TaskLabelIndexColumns = ["task_id", "label_id"];
    private static readonly string[] TasksProjectIdKeyIndexColumns = ["project_id", "key"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "task_sequences",
            schema: "delivery",
            columns: table => new
            {
                project_id = table.Column<Guid>(type: "uuid", nullable: false),
                next_number = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_task_sequences", x => x.project_id);
            });

        migrationBuilder.CreateTable(
            name: "tasks",
            schema: "delivery",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                project_id = table.Column<Guid>(type: "uuid", nullable: false),
                sprint_id = table.Column<Guid>(type: "uuid", nullable: true),
                key = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                points = table.Column<int>(type: "integer", nullable: true),
                assignee_id = table.Column<Guid>(type: "uuid", nullable: true),
                due_date = table.Column<DateOnly>(type: "date", nullable: true),
                rank = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                business_value = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_tasks", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "subtask",
            schema: "delivery",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                task_id = table.Column<Guid>(type: "uuid", nullable: false),
                title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                is_done = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_subtask", x => x.id);
                table.ForeignKey(
                    name: "fk_subtask_tasks_task_id",
                    column: x => x.task_id,
                    principalSchema: "delivery",
                    principalTable: "tasks",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "task_comment",
            schema: "delivery",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                task_id = table.Column<Guid>(type: "uuid", nullable: false),
                author_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                body = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_task_comment", x => x.id);
                table.ForeignKey(
                    name: "fk_task_comment_tasks_task_id",
                    column: x => x.task_id,
                    principalSchema: "delivery",
                    principalTable: "tasks",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "task_label",
            schema: "delivery",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                task_id = table.Column<Guid>(type: "uuid", nullable: false),
                label_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_task_label", x => x.id);
                table.ForeignKey(
                    name: "fk_task_label_tasks_task_id",
                    column: x => x.task_id,
                    principalSchema: "delivery",
                    principalTable: "tasks",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "task_link",
            schema: "delivery",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                task_id = table.Column<Guid>(type: "uuid", nullable: false),
                linked_task_id = table.Column<Guid>(type: "uuid", nullable: false),
                type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_task_link", x => x.id);
                table.ForeignKey(
                    name: "fk_task_link_tasks_task_id",
                    column: x => x.task_id,
                    principalSchema: "delivery",
                    principalTable: "tasks",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_subtask_task_id",
            schema: "delivery",
            table: "subtask",
            column: "task_id");

        migrationBuilder.CreateIndex(
            name: "ix_task_comment_task_id",
            schema: "delivery",
            table: "task_comment",
            column: "task_id");

        migrationBuilder.CreateIndex(
            name: "ix_task_label_task_id_label_id",
            schema: "delivery",
            table: "task_label",
            columns: TaskLabelIndexColumns,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_task_link_task_id",
            schema: "delivery",
            table: "task_link",
            column: "task_id");

        migrationBuilder.CreateIndex(
            name: "ix_tasks_project_id",
            schema: "delivery",
            table: "tasks",
            column: "project_id");

        migrationBuilder.CreateIndex(
            name: "ix_tasks_project_id_key",
            schema: "delivery",
            table: "tasks",
            columns: TasksProjectIdKeyIndexColumns,
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "subtask",
            schema: "delivery");

        migrationBuilder.DropTable(
            name: "task_comment",
            schema: "delivery");

        migrationBuilder.DropTable(
            name: "task_label",
            schema: "delivery");

        migrationBuilder.DropTable(
            name: "task_link",
            schema: "delivery");

        migrationBuilder.DropTable(
            name: "task_sequences",
            schema: "delivery");

        migrationBuilder.DropTable(
            name: "tasks",
            schema: "delivery");
    }
}
