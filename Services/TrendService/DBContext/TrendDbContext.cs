using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TrendService.Models;

namespace TrendService.DBContext;

public partial class TrendDbContext : DbContext
{
    public TrendDbContext()
    {
    }

    public TrendDbContext(DbContextOptions<TrendDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<JournalTrendSnapshot> JournalTrendSnapshots { get; set; }

    public virtual DbSet<ReportCache> ReportCaches { get; set; }

    public virtual DbSet<SearchHistory> SearchHistories { get; set; }

    public virtual DbSet<TrendSnapshot> TrendSnapshots { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<JournalTrendSnapshot>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("journal_trend_snapshots_pkey");

            entity.ToTable("journal_trend_snapshots");

            entity.HasIndex(e => e.JournalId, "idx_journal_trend_id");

            entity.HasIndex(e => e.Year, "idx_journal_trend_yr");

            entity.HasIndex(e => new { e.JournalId, e.Year }, "uq_journal_trend").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CitationSum)
                .HasDefaultValue(0)
                .HasColumnName("citation_sum");
            entity.Property(e => e.GrowthRate).HasColumnName("growth_rate");
            entity.Property(e => e.JournalId).HasColumnName("journal_id");
            entity.Property(e => e.JournalName)
                .HasMaxLength(500)
                .HasColumnName("journal_name");
            entity.Property(e => e.PaperCount)
                .HasDefaultValue(0)
                .HasColumnName("paper_count");
            entity.Property(e => e.RecordedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("recorded_at");
            entity.Property(e => e.Year).HasColumnName("year");
        });

        modelBuilder.Entity<ReportCache>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("report_cache_pkey");

            entity.ToTable("report_cache");

            entity.HasIndex(e => e.ExpiresAt, "idx_report_expires");

            entity.HasIndex(e => new { e.ReportType, e.ParamsHash }, "idx_report_lookup");

            entity.HasIndex(e => new { e.ReportType, e.ParamsHash }, "uq_report_cache").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.GeneratedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("generated_at");
            entity.Property(e => e.ParamsHash)
                .HasMaxLength(64)
                .HasColumnName("params_hash");
            entity.Property(e => e.ReportType)
                .HasMaxLength(100)
                .HasColumnName("report_type");
            entity.Property(e => e.ResultJson)
                .HasColumnType("jsonb")
                .HasColumnName("result_json");
        });

        modelBuilder.Entity<SearchHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("search_history_pkey");

            entity.ToTable("search_history");

            entity.HasIndex(e => e.CreatedAt, "idx_search_created").IsDescending();

            entity.HasIndex(e => e.UserId, "idx_search_user");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Query).HasColumnName("query");
            entity.Property(e => e.ResultCount).HasColumnName("result_count");
            entity.Property(e => e.SearchType)
                .HasMaxLength(50)
                .HasColumnName("search_type");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<TrendSnapshot>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("trend_snapshots_pkey");

            entity.ToTable("trend_snapshots");

            entity.HasIndex(e => e.KeywordId, "idx_trend_kw_id");

            entity.HasIndex(e => e.KeywordTerm, "idx_trend_kw_term");

            entity.HasIndex(e => e.Year, "idx_trend_year");

            entity.HasIndex(e => new { e.KeywordId, e.Year }, "uq_keyword_trend").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CitationSum)
                .HasDefaultValue(0)
                .HasColumnName("citation_sum");
            entity.Property(e => e.GrowthRate).HasColumnName("growth_rate");
            entity.Property(e => e.KeywordId).HasColumnName("keyword_id");
            entity.Property(e => e.KeywordTerm)
                .HasMaxLength(255)
                .HasColumnName("keyword_term");
            entity.Property(e => e.PaperCount)
                .HasDefaultValue(0)
                .HasColumnName("paper_count");
            entity.Property(e => e.RecordedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("recorded_at");
            entity.Property(e => e.Year).HasColumnName("year");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
