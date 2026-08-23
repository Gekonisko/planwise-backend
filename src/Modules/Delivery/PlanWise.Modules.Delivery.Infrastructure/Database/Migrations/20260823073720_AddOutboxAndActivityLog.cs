using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanWise.Modules.Delivery.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddOutboxAndActivityLog : Migration
{
    private static readonly string[] ActivityLogEntriesIndexColumns = ["project_id", "occurred_at_utc"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "activity_log_entries",
            schema: "delivery",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                project_id = table.Column<Guid>(type: "uuid", nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_activity_log_entries", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "outbox_messages",
            schema: "delivery",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                type = table.Column<string>(type: "text", nullable: false),
                content = table.Column<string>(type: "jsonb", maxLength: 2000, nullable: false),
                occurred_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                processed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                error = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_outbox_messages", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_activity_log_entries_project_id_occurred_at_utc",
            schema: "delivery",
            table: "activity_log_entries",
            columns: ActivityLogEntriesIndexColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "activity_log_entries",
            schema: "delivery");

        migrationBuilder.DropTable(
            name: "outbox_messages",
            schema: "delivery");
    }
}
