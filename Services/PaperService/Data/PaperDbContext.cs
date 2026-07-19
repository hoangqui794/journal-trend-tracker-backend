using Microsoft.EntityFrameworkCore;
using PaperService.Entities;

namespace PaperService.Data
{
    public class PaperDbContext : DbContext
    {
        public PaperDbContext(DbContextOptions<PaperDbContext> options) : base(options)
        {
        }

        public DbSet<Paper> Papers { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Keyword> Keywords { get; set; }
        public DbSet<Journal> Journals { get; set; }
        public DbSet<PaperAuthor> PaperAuthors { get; set; }
        public DbSet<PaperKeyword> PaperKeywords { get; set; }
        public DbSet<ApiSyncJob> ApiSyncJobs { get; set; }
        public DbSet<SyncCursor> SyncCursors { get; set; }
        public DbSet<SyncError> SyncErrors { get; set; }
        public DbSet<ResearchMatrix> ResearchMatrices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Postgres Enums
            modelBuilder.HasPostgresEnum<PaperSource>();
            modelBuilder.HasPostgresEnum<KeywordSource>();
            modelBuilder.HasPostgresEnum<SyncStatus>();

            // Configure Many-to-Many for Paper <-> Author
            modelBuilder.Entity<PaperAuthor>()
                .HasKey(pa => new { pa.PaperId, pa.AuthorId });

            modelBuilder.Entity<PaperAuthor>()
                .HasOne(pa => pa.Paper)
                .WithMany(p => p.PaperAuthors)
                .HasForeignKey(pa => pa.PaperId);

            modelBuilder.Entity<PaperAuthor>()
                .HasOne(pa => pa.Author)
                .WithMany(a => a.PaperAuthors)
                .HasForeignKey(pa => pa.AuthorId);

            // Configure Many-to-Many for Paper <-> Keyword
            modelBuilder.Entity<PaperKeyword>()
                .HasKey(pk => new { pk.PaperId, pk.KeywordId });

            modelBuilder.Entity<PaperKeyword>()
                .HasOne(pk => pk.Paper)
                .WithMany(p => p.PaperKeywords)
                .HasForeignKey(pk => pk.PaperId);

            modelBuilder.Entity<PaperKeyword>()
                .HasOne(pk => pk.Keyword)
                .WithMany(k => k.PaperKeywords)
                .HasForeignKey(pk => pk.KeywordId);

            // Constraints and Indexes based on SQL script
            modelBuilder.Entity<Journal>().HasIndex(j => j.ExternalId).IsUnique();
            modelBuilder.Entity<Author>().HasIndex(a => a.ExternalId).IsUnique();
            modelBuilder.Entity<Author>().HasIndex(a => a.Orcid);
            modelBuilder.Entity<Keyword>().HasIndex(k => k.Term).IsUnique();
            modelBuilder.Entity<Keyword>().HasIndex(k => k.NormalizedTerm);
            
            modelBuilder.Entity<Paper>()
                .HasIndex(p => new { p.ExternalId, p.Source })
                .IsUnique();

            modelBuilder.Entity<Paper>().HasIndex(p => p.ExternalId);
            modelBuilder.Entity<Paper>().HasIndex(p => p.PublicationYear);
            modelBuilder.Entity<Paper>().HasIndex(p => p.JournalId);
            modelBuilder.Entity<Paper>().HasIndex(p => p.Doi);

            modelBuilder.Entity<SyncCursor>().HasIndex(sc => sc.SourceName).IsUnique();
            
            modelBuilder.Entity<ApiSyncJob>().HasIndex(j => j.Status);
            modelBuilder.Entity<ApiSyncJob>().HasIndex(j => j.SourceName);
        }
    }
}
