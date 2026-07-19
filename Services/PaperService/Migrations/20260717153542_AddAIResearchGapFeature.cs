using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaperService.Migrations
{
    /// <inheritdoc />
    public partial class AddAIResearchGapFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "full_text",
                table: "papers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pdf_url",
                table: "papers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "research_matrices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_idea_prompt = table.Column<string>(type: "text", nullable: false),
                    matrix_data = table.Column<string>(type: "jsonb", nullable: false),
                    paper_ids = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_matrices", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "research_matrices");

            migrationBuilder.DropColumn(
                name: "full_text",
                table: "papers");

            migrationBuilder.DropColumn(
                name: "pdf_url",
                table: "papers");
        }
    }
}
