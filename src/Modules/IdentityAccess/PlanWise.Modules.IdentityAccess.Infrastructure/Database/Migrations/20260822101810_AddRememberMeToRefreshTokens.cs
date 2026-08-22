using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanWise.Modules.IdentityAccess.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddRememberMeToRefreshTokens : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "remember_me",
            schema: "identity_access",
            table: "refresh_tokens",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "remember_me",
            schema: "identity_access",
            table: "refresh_tokens");
    }
}
