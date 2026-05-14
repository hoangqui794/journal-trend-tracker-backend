using AIChatService.Models;
using Microsoft.EntityFrameworkCore;

namespace AIChatService.Data
{
    public class AIChatDbContext : DbContext
    {
        public AIChatDbContext(DbContextOptions<AIChatDbContext> options) : base(options)
        {
        }

        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure many-to-one relationship
            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Session)
                .WithMany(s => s.Messages)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
