using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanWise.Modules.WorkspaceManagement.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddClientNameAndPendingMembers : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "client_name",
            schema: "workspace_management",
            table: "projects",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "user_id",
            schema: "workspace_management",
            table: "project_members",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "client_name",
            schema: "workspace_management",
            table: "projects");

        migrationBuilder.AlterColumn<Guid>(
            name: "user_id",
            schema: "workspace_management",
            table: "project_members",
            type: "uuid",
            nullable: false,
            defaultValue: Guid.Empty,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);
    }
}
