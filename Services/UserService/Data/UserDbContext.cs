using Microsoft.EntityFrameworkCore;
using UserService.Models;

namespace UserService.Data
{
    public class UserDbContext : DbContext
    {
        public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
        {
        }

        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Bookmark> Bookmarks { get; set; }
        public DbSet<Follow> Follows { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<EmailQueue> EmailQueues { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Register PostgreSQL ENUM types
            modelBuilder.HasPostgresEnum<BookmarkEntity>("bookmark_entity");
            modelBuilder.HasPostgresEnum<FollowType>("follow_type");
            modelBuilder.HasPostgresEnum<NotificationType>("notification_type");
            modelBuilder.HasPostgresEnum<DeliveryStatus>("delivery_status");

            // user_profiles
            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.ResearchFields)
                    .HasColumnType("text[]");
            });

            // bookmarks
            modelBuilder.Entity<Bookmark>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.HasIndex(e => new { e.UserId, e.EntityType, e.EntityId })
                    .IsUnique()
                    .HasDatabaseName("uq_bookmark");
            });

            // follows
            modelBuilder.Entity<Follow>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.HasIndex(e => new { e.UserId, e.FollowType, e.TargetId })
                    .IsUnique()
                    .HasDatabaseName("uq_follow");
            });

            // notifications
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
            });

            // email_queue
            modelBuilder.Entity<EmailQueue>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.Attempts).HasColumnType("smallint");
            });
        }
    }
}
