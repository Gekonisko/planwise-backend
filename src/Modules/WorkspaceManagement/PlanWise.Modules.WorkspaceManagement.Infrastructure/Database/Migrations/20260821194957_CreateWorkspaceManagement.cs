using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanWise.Modules.WorkspaceManagement.Infrastructure.Database.Migrations;
    /// <inheritdoc />
    public partial class CreateWorkspaceManagement : Migration
    {
        private static readonly string[] ProjectLabelIndexColumns = ["project_id", "name"];
        private static readonly string[] ProjectMemberIndexColumns = ["project_id", "user_id"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "workspace_management");

            migrationBuilder.CreateTable(
                name: "projects",
                schema: "workspace_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    key_prefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    process = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "project_labels",
                schema: "workspace_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_labels", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_labels_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "workspace_management",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_members",
                schema: "workspace_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    capacity = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    hourly_rate = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_members_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "workspace_management",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_project_labels_project_id_name",
                schema: "workspace_management",
                table: "project_labels",
                columns: ProjectLabelIndexColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_project_members_project_id_user_id",
                schema: "workspace_management",
                table: "project_members",
                columns: ProjectMemberIndexColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_projects_key_prefix",
                schema: "workspace_management",
                table: "projects",
                column: "key_prefix",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "project_labels",
                schema: "workspace_management");

            migrationBuilder.DropTable(
                name: "project_members",
                schema: "workspace_management");

            migrationBuilder.DropTable(
                name: "projects",
                schema: "workspace_management");
        }
}
