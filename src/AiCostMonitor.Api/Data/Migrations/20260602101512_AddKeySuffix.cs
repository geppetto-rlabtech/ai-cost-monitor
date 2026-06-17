using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCostMonitor.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKeySuffix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "cost_usd",
                table: "usage_records",
                type: "numeric(18,8)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<string>(
                name: "key_suffix",
                table: "provider_keys",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "key_suffix",
                table: "provider_keys");

            migrationBuilder.AlterColumn<decimal>(
                name: "cost_usd",
                table: "usage_records",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,8)");
        }
    }
}
