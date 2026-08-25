using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanWise.Modules.WorkspaceManagement.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddMemberSkills : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string[]>(
            name: "skills",
            schema: "workspace_management",
            table: "project_members",
            type: "text[]",
            nullable: false,
            defaultValue: Array.Empty<string>());
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "skills",
            schema: "workspace_management",
            table: "project_members");
    }
}
