using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserService.DTOs;
using UserService.Models;
using UserService.Repositories;

namespace UserService.Services
{
    public class UserAppService : IUserAppService
    {
        private readonly IUserRepository _repository;
        private readonly IIdentityClient _identityClient;

        public UserAppService(IUserRepository repository, IIdentityClient identityClient)
        {
            _repository = repository;
            _identityClient = identityClient;
        }

        // ── PROFILES ─────────────────────────────────────────
        public async Task<UserProfileDto?> GetProfileAsync(Guid userId)
        {
            var profile = await _repository.GetProfileAsync(userId);
            if (profile == null) return null;
            
            var dto = MapToProfileDto(profile);
            
            try
            {
                var identity = await _identityClient.GetUserAsync(userId);
                if (identity != null)
                {
                    dto.FullName = identity.FullName;
                    dto.Email = identity.Email;
                }
            }
            catch (Exception ex)
            {
                // Log exception but return the profile anyway to prevent total failure if IdentityService has issues
                Console.WriteLine($"[UserAppService Warning] Failed to fetch identity details for user {userId}: {ex.Message}");
            }
            
            return dto;
        }

        public async Task UpdateProfileAsync(Guid userId, UserProfileUpdateDto dto)
        {
            var userExists = await _identityClient.ValidateUserExistsAsync(userId);
            if (!userExists)
            {
                throw new ArgumentException($"User with ID {userId} does not exist in IdentityService.");
            }

            // Forward FullName and Email updates to IdentityService if they are provided
            if (!string.IsNullOrEmpty(dto.FullName) || !string.IsNullOrEmpty(dto.Email))
            {
                var updateSuccess = await _identityClient.UpdateUserAsync(userId, dto.FullName ?? "", dto.Email ?? "");
                if (!updateSuccess)
                {
                    Console.WriteLine($"[UserAppService Warning] Failed to update user details (FullName/Email) in IdentityService for user {userId}");
                }
            }

            var profile = new UserProfile
            {
                UserId = userId,
                Bio = dto.Bio,
                Institution = dto.Institution,
                ResearchFields = dto.ResearchFields,
                WebsiteUrl = dto.WebsiteUrl
            };
            await _repository.UpsertProfileAsync(profile);
        }

        // ── BOOKMARKS ─────────────────────────────────────────
        public async Task<IEnumerable<BookmarkDto>> GetBookmarksAsync(Guid userId, string? entityType)
        {
            BookmarkEntity? filter = null;
            if (!string.IsNullOrEmpty(entityType) && Enum.TryParse<BookmarkEntity>(entityType, out var parsed))
                filter = parsed;

            var bookmarks = await _repository.GetBookmarksAsync(userId, filter);
            return bookmarks.Select(b => new BookmarkDto
            {
                Id = b.Id,
                EntityType = b.EntityType.ToString(),
                EntityId = b.EntityId,
                EntityTitle = b.EntityTitle,
                Note = b.Note,
                CreatedAt = b.CreatedAt
            });
        }

        public async Task CreateBookmarkAsync(Guid userId, BookmarkCreateDto dto)
        {
            if (!Enum.TryParse<BookmarkEntity>(dto.EntityType, out var entityType))
                throw new ArgumentException($"Invalid entity_type: {dto.EntityType}. Must be 'paper', 'keyword', or 'journal'.");

            var bookmark = new Bookmark
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EntityType = entityType,
                EntityId = dto.EntityId,
                EntityTitle = dto.EntityTitle,
                Note = dto.Note,
                CreatedAt = DateTime.UtcNow
            };
            await _repository.CreateBookmarkAsync(bookmark);
        }

        public async Task DeleteBookmarkAsync(Guid id, Guid userId)
            => await _repository.DeleteBookmarkAsync(id, userId);

        // ── FOLLOWS ───────────────────────────────────────────
        public async Task<IEnumerable<FollowDto>> GetFollowsAsync(Guid userId)
        {
            var follows = await _repository.GetFollowsAsync(userId);
            return follows.Select(f => new FollowDto
            {
                Id = f.Id,
                FollowType = f.FollowType.ToString(),
                TargetId = f.TargetId,
                TargetName = f.TargetName,
                NotifyEmail = f.NotifyEmail,
                NotifyInapp = f.NotifyInapp,
                CreatedAt = f.CreatedAt
            });
        }

        public async Task FollowKeywordAsync(Guid userId, Guid keywordId, string? targetName)
        {
            var follow = new Follow
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FollowType = FollowType.keyword,
                TargetId = keywordId,
                TargetName = targetName,
                NotifyEmail = true,
                NotifyInapp = true,
                CreatedAt = DateTime.UtcNow
            };
            await _repository.CreateFollowAsync(follow);
        }

        public async Task FollowJournalAsync(Guid userId, Guid journalId, string? targetName)
        {
            var follow = new Follow
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FollowType = FollowType.journal,
                TargetId = journalId,
                TargetName = targetName,
                NotifyEmail = true,
                NotifyInapp = true,
                CreatedAt = DateTime.UtcNow
            };
            await _repository.CreateFollowAsync(follow);
        }

        public async Task UnfollowAsync(Guid id, Guid userId)
            => await _repository.DeleteFollowAsync(id, userId);

        // ── NOTIFICATIONS ─────────────────────────────────────
        public async Task<IEnumerable<NotificationDto>> GetNotificationsAsync(Guid userId, bool? isRead)
        {
            var notifications = await _repository.GetNotificationsAsync(userId, isRead);
            return notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type.ToString(),
                Title = n.Title,
                Body = n.Body,
                RelatedId = n.RelatedId,
                RelatedType = n.RelatedType,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                CreatedAt = n.CreatedAt
            });
        }

        public async Task MarkAsReadAsync(Guid id, Guid userId)
            => await _repository.MarkNotificationAsReadAsync(id, userId);

        public async Task MarkAllAsReadAsync(Guid userId)
            => await _repository.MarkAllAsReadAsync(userId);

        // Triggered by PaperService when a new paper matches a keyword
        public async Task TriggerNotificationsAsync(NotificationTriggerDto trigger)
        {
            // Find all users following this keyword
            var followers = await _repository.GetFollowersByTargetAsync(trigger.KeywordId, FollowType.keyword);

            foreach (var follow in followers)
            {
                // 1. Create in-app notification (if notify_inapp = true)
                if (follow.NotifyInapp)
                {
                    var notification = new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = follow.UserId,
                        Type = NotificationType.new_paper,
                        Title = trigger.Title,
                        Body = trigger.Body,
                        RelatedId = trigger.RelatedPaperId,
                        RelatedType = trigger.RelatedPaperId.HasValue ? "paper" : null,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _repository.CreateNotificationAsync(notification);
                }

                // 2. Queue email (if notify_email = true)
                if (follow.NotifyEmail)
                {
                    var user = await _identityClient.GetUserAsync(follow.UserId);
                    var toEmail = user?.Email ?? $"user-{follow.UserId}@placeholder.com";

                    var email = new EmailQueue
                    {
                        Id = Guid.NewGuid(),
                        UserId = follow.UserId,
                        ToEmail = toEmail,
                        Subject = trigger.Title,
                        BodyHtml = $"<p>{trigger.Body}</p>",
                        Status = DeliveryStatus.pending,
                        Attempts = 0,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _repository.AddToEmailQueueAsync(email);
                }
            }
        }

        // ── HELPERS ───────────────────────────────────────────
        private static UserProfileDto MapToProfileDto(UserProfile p) => new()
        {
            UserId = p.UserId,
            Bio = p.Bio,
            Institution = p.Institution,
            ResearchFields = p.ResearchFields,
            WebsiteUrl = p.WebsiteUrl
        };
    }
}
