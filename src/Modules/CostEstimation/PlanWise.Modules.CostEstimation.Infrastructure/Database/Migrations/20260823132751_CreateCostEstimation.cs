using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanWise.Modules.CostEstimation.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class CreateCostEstimation : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "cost_estimation");

        migrationBuilder.CreateTable(
            name: "budgets",
            schema: "cost_estimation",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                project_id = table.Column<Guid>(type: "uuid", nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_budgets", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "cost_estimate_runs",
            schema: "cost_estimation",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                project_id = table.Column<Guid>(type: "uuid", nullable: false),
                job_id = table.Column<Guid>(type: "uuid", nullable: false),
                model_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                input_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                result_json = table.Column<string>(type: "jsonb", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_cost_estimate_runs", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_cost_estimate_runs_project_id",
            schema: "cost_estimation",
            table: "cost_estimate_runs",
            column: "project_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "budgets",
            schema: "cost_estimation");

        migrationBuilder.DropTable(
            name: "cost_estimate_runs",
            schema: "cost_estimation");
    }
}
