using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanWise.Common.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class CreateAsyncJobs : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "common");

        migrationBuilder.CreateTable(
            name: "async_jobs",
            schema: "common",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                job_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                project_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                result_location = table.Column<string>(type: "text", nullable: true),
                error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_async_jobs", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_async_jobs_project_id",
            schema: "common",
            table: "async_jobs",
            column: "project_id");

        migrationBuilder.CreateIndex(
            name: "ix_async_jobs_status",
            schema: "common",
            table: "async_jobs",
            column: "status");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "async_jobs",
            schema: "common");
    }
}
