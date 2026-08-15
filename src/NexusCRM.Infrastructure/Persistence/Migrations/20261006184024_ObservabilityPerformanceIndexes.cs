using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ObservabilityPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_leads_TenantId_CompanyName",
                table: "leads",
                columns: new[] { "TenantId", "CompanyName" });

            migrationBuilder.CreateIndex(
                name: "IX_leads_TenantId_Email",
                table: "leads",
                columns: new[] { "TenantId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_leads_TenantId_Status_CreatedAtUtc",
                table: "leads",
                columns: new[] { "TenantId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_deals_TenantId_Status_CreatedAtUtc",
                table: "deals",
                columns: new[] { "TenantId", "Status", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_leads_TenantId_CompanyName",
                table: "leads");

            migrationBuilder.DropIndex(
                name: "IX_leads_TenantId_Email",
                table: "leads");

            migrationBuilder.DropIndex(
                name: "IX_leads_TenantId_Status_CreatedAtUtc",
                table: "leads");

            migrationBuilder.DropIndex(
                name: "IX_deals_TenantId_Status_CreatedAtUtc",
                table: "deals");
        }
    }
}
