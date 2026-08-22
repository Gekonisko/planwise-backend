using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanWise.Modules.Delivery.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class CreateDelivery : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "delivery");

        migrationBuilder.CreateTable(
            name: "sprints",
            schema: "delivery",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                project_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                goal = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                start_date = table.Column<DateOnly>(type: "date", nullable: false),
                end_date = table.Column<DateOnly>(type: "date", nullable: false),
                state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_sprints", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_sprints_project_id",
            schema: "delivery",
            table: "sprints",
            column: "project_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "sprints",
            schema: "delivery");
    }
}
