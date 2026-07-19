using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using PaperService.Entities;

#nullable disable

namespace PaperService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:keyword_source", "user,api")
                .Annotation("Npgsql:Enum:paper_source", "openalex,semantic_scholar,crossref")
                .Annotation("Npgsql:Enum:sync_status", "running,success,failed,cancelled");

            migrationBuilder.CreateTable(
                name: "api_sync_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_base_url = table.Column<string>(type: "text", nullable: false),
                    query_params = table.Column<string>(type: "jsonb", nullable: true),
                    scheduled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<SyncStatus>(type: "sync_status", nullable: false),
                    papers_fetched = table.Column<int>(type: "integer", nullable: false),
                    papers_inserted = table.Column<int>(type: "integer", nullable: false),
                    papers_updated = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_sync_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "authors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    affiliation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    orcid = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_authors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "journals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    issn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    e_issn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    publisher = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    field = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    homepage_url = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "keywords",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    term = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    normalized_term = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    source = table.Column<KeywordSource>(type: "keyword_source", nullable: false),
                    usage_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_keywords", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sync_cursors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_cursor = table.Column<string>(type: "text", nullable: true),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_cursors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sync_errors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    error_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    error_detail = table.Column<string>(type: "text", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_errors", x => x.id);
                    table.ForeignKey(
                        name: "FK_sync_errors_api_sync_jobs_job_id",
                        column: x => x.job_id,
                        principalTable: "api_sync_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "papers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    source = table.Column<PaperSource>(type: "paper_source", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    @abstract = table.Column<string>(name: "abstract", type: "text", nullable: true),
                    publication_year = table.Column<short>(type: "smallint", nullable: true),
                    doi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    url = table.Column<string>(type: "text", nullable: true),
                    citation_count = table.Column<int>(type: "integer", nullable: false),
                    reference_count = table.Column<int>(type: "integer", nullable: false),
                    fields_of_study = table.Column<List<string>>(type: "text[]", nullable: true),
                    journal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    raw_data = table.Column<string>(type: "jsonb", nullable: true),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_papers", x => x.id);
                    table.ForeignKey(
                        name: "FK_papers_journals_journal_id",
                        column: x => x.journal_id,
                        principalTable: "journals",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "paper_authors",
                columns: table => new
                {
                    paper_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_order = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paper_authors", x => new { x.paper_id, x.author_id });
                    table.ForeignKey(
                        name: "FK_paper_authors_authors_author_id",
                        column: x => x.author_id,
                        principalTable: "authors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_paper_authors_papers_paper_id",
                        column: x => x.paper_id,
                        principalTable: "papers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "paper_keywords",
                columns: table => new
                {
                    paper_id = table.Column<Guid>(type: "uuid", nullable: false),
                    keyword_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relevance_score = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paper_keywords", x => new { x.paper_id, x.keyword_id });
                    table.ForeignKey(
                        name: "FK_paper_keywords_keywords_keyword_id",
                        column: x => x.keyword_id,
                        principalTable: "keywords",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_paper_keywords_papers_paper_id",
                        column: x => x.paper_id,
                        principalTable: "papers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_api_sync_jobs_source_name",
                table: "api_sync_jobs",
                column: "source_name");

            migrationBuilder.CreateIndex(
                name: "IX_api_sync_jobs_status",
                table: "api_sync_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_authors_external_id",
                table: "authors",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_authors_orcid",
                table: "authors",
                column: "orcid");

            migrationBuilder.CreateIndex(
                name: "IX_journals_external_id",
                table: "journals",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_keywords_normalized_term",
                table: "keywords",
                column: "normalized_term");

            migrationBuilder.CreateIndex(
                name: "IX_keywords_term",
                table: "keywords",
                column: "term",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_paper_authors_author_id",
                table: "paper_authors",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_paper_keywords_keyword_id",
                table: "paper_keywords",
                column: "keyword_id");

            migrationBuilder.CreateIndex(
                name: "IX_papers_doi",
                table: "papers",
                column: "doi");

            migrationBuilder.CreateIndex(
                name: "IX_papers_external_id",
                table: "papers",
                column: "external_id");

            migrationBuilder.CreateIndex(
                name: "IX_papers_external_id_source",
                table: "papers",
                columns: new[] { "external_id", "source" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_papers_journal_id",
                table: "papers",
                column: "journal_id");

            migrationBuilder.CreateIndex(
                name: "IX_papers_publication_year",
                table: "papers",
                column: "publication_year");

            migrationBuilder.CreateIndex(
                name: "IX_sync_cursors_source_name",
                table: "sync_cursors",
                column: "source_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sync_errors_job_id",
                table: "sync_errors",
                column: "job_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "paper_authors");

            migrationBuilder.DropTable(
                name: "paper_keywords");

            migrationBuilder.DropTable(
                name: "sync_cursors");

            migrationBuilder.DropTable(
                name: "sync_errors");

            migrationBuilder.DropTable(
                name: "authors");

            migrationBuilder.DropTable(
                name: "keywords");

            migrationBuilder.DropTable(
                name: "papers");

            migrationBuilder.DropTable(
                name: "api_sync_jobs");

            migrationBuilder.DropTable(
                name: "journals");
        }
    }
}
