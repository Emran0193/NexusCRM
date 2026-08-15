using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CrmCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "row_version",
                table: "customers",
                type: "bytea",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldDefaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "customer_notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customer_notes_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer_timeline",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_timeline", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customer_timeline_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "deals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PipelineId = table.Column<Guid>(type: "uuid", nullable: false),
                    StageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    LeadId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpectedCloseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "leads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PipelineId = table.Column<Guid>(type: "uuid", nullable: false),
                    StageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    tags = table.Column<List<string>>(type: "text[]", nullable: false),
                    row_version = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pipelines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pipelines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pipeline_stages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsWon = table.Column<bool>(type: "boolean", nullable: false),
                    IsLost = table.Column<bool>(type: "boolean", nullable: false),
                    WinProbability = table.Column<int>(type: "integer", nullable: true),
                    PipelineId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pipeline_stages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pipeline_stages_pipelines_PipelineId",
                        column: x => x.PipelineId,
                        principalTable: "pipelines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_notes_CustomerId",
                table: "customer_notes",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_timeline_CustomerId",
                table: "customer_timeline",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_timeline_OccurredAtUtc",
                table: "customer_timeline",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_deals_PipelineId",
                table: "deals",
                column: "PipelineId");

            migrationBuilder.CreateIndex(
                name: "IX_deals_TenantId_StageId",
                table: "deals",
                columns: new[] { "TenantId", "StageId" });

            migrationBuilder.CreateIndex(
                name: "IX_deals_TenantId_Title",
                table: "deals",
                columns: new[] { "TenantId", "Title" });

            migrationBuilder.CreateIndex(
                name: "IX_leads_PipelineId",
                table: "leads",
                column: "PipelineId");

            migrationBuilder.CreateIndex(
                name: "IX_leads_TenantId_StageId",
                table: "leads",
                columns: new[] { "TenantId", "StageId" });

            migrationBuilder.CreateIndex(
                name: "IX_leads_TenantId_Title",
                table: "leads",
                columns: new[] { "TenantId", "Title" });

            migrationBuilder.CreateIndex(
                name: "IX_pipeline_stages_PipelineId",
                table: "pipeline_stages",
                column: "PipelineId");

            migrationBuilder.CreateIndex(
                name: "IX_pipeline_stages_SortOrder",
                table: "pipeline_stages",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_pipelines_TenantId_Type_IsDefault",
                table: "pipelines",
                columns: new[] { "TenantId", "Type", "IsDefault" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_notes");

            migrationBuilder.DropTable(
                name: "customer_timeline");

            migrationBuilder.DropTable(
                name: "deals");

            migrationBuilder.DropTable(
                name: "leads");

            migrationBuilder.DropTable(
                name: "pipeline_stages");

            migrationBuilder.DropTable(
                name: "pipelines");

            migrationBuilder.AlterColumn<byte[]>(
                name: "row_version",
                table: "customers",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "bytea");
        }
    }
}
