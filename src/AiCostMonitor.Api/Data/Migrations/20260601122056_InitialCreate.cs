using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCostMonitor.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "provider_keys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "text", nullable: false),
                    encrypted_api_key = table.Column<string>(type: "text", nullable: false),
                    label = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_provider_keys", x => x.id);
                    table.ForeignKey(
                        name: "fk_provider_keys_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usage_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_key_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "text", nullable: false),
                    model = table.Column<string>(type: "text", nullable: false),
                    period_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    period_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    input_tokens = table.Column<long>(type: "bigint", nullable: false),
                    output_tokens = table.Column<long>(type: "bigint", nullable: false),
                    cache_read_tokens = table.Column<long>(type: "bigint", nullable: false),
                    cache_creation_tokens = table.Column<long>(type: "bigint", nullable: false),
                    cost_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    raw_json = table.Column<string>(type: "text", nullable: true),
                    synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usage_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_usage_records_provider_keys_provider_key_id",
                        column: x => x.provider_key_id,
                        principalTable: "provider_keys",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_usage_records_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_provider_keys_user_id_provider_label",
                table: "provider_keys",
                columns: new[] { "user_id", "provider", "label" });

            migrationBuilder.CreateIndex(
                name: "ix_usage_records_provider_key_id_model_period_start",
                table: "usage_records",
                columns: new[] { "provider_key_id", "model", "period_start" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usage_records_provider_model",
                table: "usage_records",
                columns: new[] { "provider", "model" });

            migrationBuilder.CreateIndex(
                name: "ix_usage_records_user_id_period_start",
                table: "usage_records",
                columns: new[] { "user_id", "period_start" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "usage_records");

            migrationBuilder.DropTable(
                name: "provider_keys");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
