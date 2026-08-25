using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanWise.Modules.Delivery.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddTaskCompletedAtUtc : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "completed_at_utc",
            schema: "delivery",
            table: "tasks",
            type: "timestamp with time zone",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "completed_at_utc",
            schema: "delivery",
            table: "tasks");
    }
}
