using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrendService.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicAndAuthorTrends : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "author_trend_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    year = table.Column<short>(type: "smallint", nullable: false),
                    paper_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    citation_sum = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    growth_rate = table.Column<double>(type: "double precision", nullable: true),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("author_trend_snapshots_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "topic_trend_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    topic_id = table.Column<Guid>(type: "uuid", nullable: false),
                    topic_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    year = table.Column<short>(type: "smallint", nullable: false),
                    paper_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    citation_sum = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    growth_rate = table.Column<double>(type: "double precision", nullable: true),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("topic_trend_snapshots_pkey", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_author_trend_id",
                table: "author_trend_snapshots",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "idx_author_trend_year",
                table: "author_trend_snapshots",
                column: "year");

            migrationBuilder.CreateIndex(
                name: "uq_author_trend",
                table: "author_trend_snapshots",
                columns: new[] { "author_id", "year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_topic_trend_id",
                table: "topic_trend_snapshots",
                column: "topic_id");

            migrationBuilder.CreateIndex(
                name: "idx_topic_trend_year",
                table: "topic_trend_snapshots",
                column: "year");

            migrationBuilder.CreateIndex(
                name: "uq_topic_trend",
                table: "topic_trend_snapshots",
                columns: new[] { "topic_id", "year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "author_trend_snapshots");

            migrationBuilder.DropTable(
                name: "topic_trend_snapshots");
        }
    }
}
