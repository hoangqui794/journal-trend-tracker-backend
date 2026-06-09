using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserService.Models;

namespace UserService.Repositories
{
    public interface IUserRepository
    {
        // Profiles
        Task<UserProfile?> GetProfileAsync(Guid userId);
        Task UpsertProfileAsync(UserProfile profile);

        // Bookmarks
        Task<IEnumerable<Bookmark>> GetBookmarksAsync(Guid userId, BookmarkEntity? entityType);
        Task CreateBookmarkAsync(Bookmark bookmark);
        Task DeleteBookmarkAsync(Guid id, Guid userId);

        // Follows
        Task<IEnumerable<Follow>> GetFollowsAsync(Guid userId);
        Task CreateFollowAsync(Follow follow);
        Task DeleteFollowAsync(Guid id, Guid userId);
        Task<IEnumerable<Follow>> GetFollowersByTargetAsync(Guid targetId, FollowType followType);

        // Notifications
        Task<IEnumerable<Notification>> GetNotificationsAsync(Guid userId, bool? isRead);
        Task CreateNotificationAsync(Notification notification);
        Task MarkNotificationAsReadAsync(Guid id, Guid userId);
        Task MarkAllAsReadAsync(Guid userId);

        // Email Queue
        Task AddToEmailQueueAsync(EmailQueue email);
        Task<IEnumerable<EmailQueue>> GetPendingEmailsAsync();
        Task UpdateEmailStatusAsync(Guid id, DeliveryStatus status, DateTime? sentAt, string? errorMessage);
    }
}
