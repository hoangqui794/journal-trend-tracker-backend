using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserService.Data;
using UserService.Models;

namespace UserService.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserDbContext _context;

        public UserRepository(UserDbContext context)
        {
            _context = context;
        }

        // ── PROFILES ─────────────────────────────────────────
        public async Task<UserProfile?> GetProfileAsync(Guid userId)
            => await _context.UserProfiles.FindAsync(userId);

        public async Task UpsertProfileAsync(UserProfile profile)
        {
            var existing = await _context.UserProfiles.FindAsync(profile.UserId);
            if (existing == null)
            {
                profile.CreatedAt = DateTime.UtcNow;
                profile.UpdatedAt = DateTime.UtcNow;
                await _context.UserProfiles.AddAsync(profile);
            }
            else
            {
                existing.Bio = profile.Bio;
                existing.Institution = profile.Institution;
                existing.ResearchFields = profile.ResearchFields;
                existing.WebsiteUrl = profile.WebsiteUrl;
                // updated_at is handled by DB trigger, but set here too for EF tracking
                existing.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
        }

        // ── BOOKMARKS ─────────────────────────────────────────
        public async Task<IEnumerable<Bookmark>> GetBookmarksAsync(Guid userId, BookmarkEntity? entityType)
        {
            var query = _context.Bookmarks.Where(b => b.UserId == userId);
            if (entityType.HasValue)
                query = query.Where(b => b.EntityType == entityType.Value);
            return await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
        }

        public async Task CreateBookmarkAsync(Bookmark bookmark)
        {
            await _context.Bookmarks.AddAsync(bookmark);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteBookmarkAsync(Guid id, Guid userId)
        {
            var bookmark = await _context.Bookmarks
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
            if (bookmark != null)
            {
                _context.Bookmarks.Remove(bookmark);
                await _context.SaveChangesAsync();
            }
        }

        // ── FOLLOWS ───────────────────────────────────────────
        public async Task<IEnumerable<Follow>> GetFollowsAsync(Guid userId)
            => await _context.Follows
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

        public async Task CreateFollowAsync(Follow follow)
        {
            await _context.Follows.AddAsync(follow);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteFollowAsync(Guid id, Guid userId)
        {
            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
            if (follow != null)
            {
                _context.Follows.Remove(follow);
                await _context.SaveChangesAsync();
            }
        }

        // Returns all users following a specific target (keyword/journal)
        public async Task<IEnumerable<Follow>> GetFollowersByTargetAsync(Guid targetId, FollowType followType)
            => await _context.Follows
                .Where(f => f.TargetId == targetId && f.FollowType == followType)
                .ToListAsync();

        // ── NOTIFICATIONS ─────────────────────────────────────
        public async Task<IEnumerable<Notification>> GetNotificationsAsync(Guid userId, bool? isRead)
        {
            var query = _context.Notifications.Where(n => n.UserId == userId);
            if (isRead.HasValue)
                query = query.Where(n => n.IsRead == isRead.Value);
            return await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
        }

        public async Task CreateNotificationAsync(Notification notification)
        {
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
        }

        public async Task MarkNotificationAsReadAsync(Guid id, Guid userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();
            var now = DateTime.UtcNow;
            foreach (var n in unread)
            {
                n.IsRead = true;
                n.ReadAt = now;
            }
            await _context.SaveChangesAsync();
        }

        // ── EMAIL QUEUE ───────────────────────────────────────
        public async Task AddToEmailQueueAsync(EmailQueue email)
        {
            await _context.EmailQueues.AddAsync(email);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<EmailQueue>> GetPendingEmailsAsync()
            => await _context.EmailQueues
                .Where(e => e.Status == DeliveryStatus.pending && e.Attempts < 3)
                .ToListAsync();

        public async Task UpdateEmailStatusAsync(Guid id, DeliveryStatus status, DateTime? sentAt, string? errorMessage)
        {
            var email = await _context.EmailQueues.FindAsync(id);
            if (email != null)
            {
                email.Status = status;
                email.SentAt = sentAt;
                email.LastAttempted = DateTime.UtcNow;
                email.Attempts += 1;
                email.ErrorMessage = errorMessage;
                await _context.SaveChangesAsync();
            }
        }
    }
}
