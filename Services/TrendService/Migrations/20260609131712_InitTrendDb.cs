using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrendService.Migrations
{
    /// <inheritdoc />
    public partial class InitTrendDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.CreateTable(
                name: "journal_trend_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    journal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    journal_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    year = table.Column<short>(type: "smallint", nullable: false),
                    paper_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    citation_sum = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    growth_rate = table.Column<double>(type: "double precision", nullable: true),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("journal_trend_snapshots_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "report_cache",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    report_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    params_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    result_json = table.Column<string>(type: "jsonb", nullable: false),
                    generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("report_cache_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "search_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    query = table.Column<string>(type: "text", nullable: false),
                    search_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    result_count = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("search_history_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "trend_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    keyword_id = table.Column<Guid>(type: "uuid", nullable: false),
                    keyword_term = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    year = table.Column<short>(type: "smallint", nullable: false),
                    paper_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    citation_sum = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    growth_rate = table.Column<double>(type: "double precision", nullable: true),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("trend_snapshots_pkey", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_journal_trend_id",
                table: "journal_trend_snapshots",
                column: "journal_id");

            migrationBuilder.CreateIndex(
                name: "idx_journal_trend_yr",
                table: "journal_trend_snapshots",
                column: "year");

            migrationBuilder.CreateIndex(
                name: "uq_journal_trend",
                table: "journal_trend_snapshots",
                columns: new[] { "journal_id", "year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_report_expires",
                table: "report_cache",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "idx_report_lookup",
                table: "report_cache",
                columns: new[] { "report_type", "params_hash" });

            migrationBuilder.CreateIndex(
                name: "uq_report_cache",
                table: "report_cache",
                columns: new[] { "report_type", "params_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_search_created",
                table: "search_history",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_search_user",
                table: "search_history",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_trend_kw_id",
                table: "trend_snapshots",
                column: "keyword_id");

            migrationBuilder.CreateIndex(
                name: "idx_trend_kw_term",
                table: "trend_snapshots",
                column: "keyword_term");

            migrationBuilder.CreateIndex(
                name: "idx_trend_year",
                table: "trend_snapshots",
                column: "year");

            migrationBuilder.CreateIndex(
                name: "uq_keyword_trend",
                table: "trend_snapshots",
                columns: new[] { "keyword_id", "year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "journal_trend_snapshots");

            migrationBuilder.DropTable(
                name: "report_cache");

            migrationBuilder.DropTable(
                name: "search_history");

            migrationBuilder.DropTable(
                name: "trend_snapshots");
        }
    }
}
