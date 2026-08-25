using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanWise.Modules.CostEstimation.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddReductionsSupport : Migration
{
    private static readonly string[] RunIdReductionIdColumns = ["run_id", "reduction_id"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "applied_reductions",
            schema: "cost_estimation",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                run_id = table.Column<Guid>(type: "uuid", nullable: false),
                reduction_id = table.Column<Guid>(type: "uuid", nullable: false),
                applied_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_applied_reductions", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_applied_reductions_run_id_reduction_id",
            schema: "cost_estimation",
            table: "applied_reductions",
            columns: RunIdReductionIdColumns,
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "applied_reductions",
            schema: "cost_estimation");
    }
}
