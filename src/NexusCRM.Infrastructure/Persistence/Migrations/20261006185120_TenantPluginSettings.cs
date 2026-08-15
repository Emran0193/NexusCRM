using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TenantPluginSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenant_plugin_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PluginId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SettingsJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_plugin_settings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_plugin_settings_TenantId_PluginId",
                table: "tenant_plugin_settings",
                columns: new[] { "TenantId", "PluginId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_plugin_settings");
        }
    }
}
