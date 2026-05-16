using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserService.DTOs;

namespace UserService.Services
{
    public interface IUserAppService
    {
        // Profiles
        Task<UserProfileDto?> GetProfileAsync(Guid userId);
        Task UpdateProfileAsync(Guid userId, UserProfileUpdateDto dto);

        // Bookmarks
        Task<IEnumerable<BookmarkDto>> GetBookmarksAsync(Guid userId, string? entityType);
        Task CreateBookmarkAsync(Guid userId, BookmarkCreateDto dto);
        Task DeleteBookmarkAsync(Guid id, Guid userId);

        // Follows
        Task<IEnumerable<FollowDto>> GetFollowsAsync(Guid userId);
        Task FollowKeywordAsync(Guid userId, Guid keywordId, string? targetName);
        Task FollowJournalAsync(Guid userId, Guid journalId, string? targetName);
        Task UnfollowAsync(Guid id, Guid userId);

        // Notifications
        Task<IEnumerable<NotificationDto>> GetNotificationsAsync(Guid userId, bool? isRead);
        Task MarkAsReadAsync(Guid id, Guid userId);
        Task MarkAllAsReadAsync(Guid userId);
        Task TriggerNotificationsAsync(NotificationTriggerDto triggerDto);
    }
}
