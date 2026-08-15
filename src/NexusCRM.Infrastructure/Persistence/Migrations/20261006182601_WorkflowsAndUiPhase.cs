using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WorkflowsAndUiPhase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workflow_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    TriggerType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TriggerType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContextJson = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ResultSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_actions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Target = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    WorkflowDefinitionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_actions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_actions_workflow_definitions_WorkflowDefinitionId",
                        column: x => x.WorkflowDefinitionId,
                        principalTable: "workflow_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_conditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Field = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Operator = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    WorkflowDefinitionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_conditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_conditions_workflow_definitions_WorkflowDefinition~",
                        column: x => x.WorkflowDefinitionId,
                        principalTable: "workflow_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_actions_WorkflowDefinitionId",
                table: "workflow_actions",
                column: "WorkflowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_conditions_WorkflowDefinitionId",
                table: "workflow_conditions",
                column: "WorkflowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_definitions_TenantId_TriggerType_IsEnabled",
                table: "workflow_definitions",
                columns: new[] { "TenantId", "TriggerType", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_runs_TenantId_Status_CreatedAtUtc",
                table: "workflow_runs",
                columns: new[] { "TenantId", "Status", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workflow_actions");

            migrationBuilder.DropTable(
                name: "workflow_conditions");

            migrationBuilder.DropTable(
                name: "workflow_runs");

            migrationBuilder.DropTable(
                name: "workflow_definitions");
        }
    }
}
