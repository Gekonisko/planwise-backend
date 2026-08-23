using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanWise.Modules.IdentityAccess.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddUserPreferences : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "board_grouping",
            schema: "identity_access",
            table: "users",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "default_project_id",
            schema: "identity_access",
            table: "users",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "wip_display",
            schema: "identity_access",
            table: "users",
            type: "boolean",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "board_grouping",
            schema: "identity_access",
            table: "users");

        migrationBuilder.DropColumn(
            name: "default_project_id",
            schema: "identity_access",
            table: "users");

        migrationBuilder.DropColumn(
            name: "wip_display",
            schema: "identity_access",
            table: "users");
    }
}
