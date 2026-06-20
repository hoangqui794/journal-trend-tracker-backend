using System;
using UserService.Models;

namespace UserService.DTOs
{
    // ── Profile DTOs ──────────────────────────────────────────
    public class UserProfileDto
    {
        public Guid UserId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Bio { get; set; }
        public string? Institution { get; set; }
        public string[]? ResearchFields { get; set; }
        public string? WebsiteUrl { get; set; }
    }

    public class UserProfileUpdateDto
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Bio { get; set; }// tieu sư
        public string? Institution { get; set; } // to chuc
        public string[]? ResearchFields { get; set; }
        public string? WebsiteUrl { get; set; }
    }

    // ── Bookmark DTOs ─────────────────────────────────────────
    public class BookmarkDto
    {
        public Guid Id { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public string? EntityTitle { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class BookmarkCreateDto
    {
        public string EntityType { get; set; } = string.Empty; // "paper" | "keyword" | "journal"
        public Guid EntityId { get; set; }
        public string? EntityTitle { get; set; }
        public string? Note { get; set; }
    }

    // ── Follow DTOs ───────────────────────────────────────────
    public class FollowDto
    {
        public Guid Id { get; set; }
        public string FollowType { get; set; } = string.Empty;
        public Guid TargetId { get; set; }
        public string? TargetName { get; set; }
        public bool NotifyEmail { get; set; }
        public bool NotifyInapp { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ── Notification DTOs ─────────────────────────────────────
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Body { get; set; }
        public Guid? RelatedId { get; set; }
        public string? RelatedType { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ── Internal Trigger DTO (from PaperService) ──────────────
    public class NotificationTriggerDto
    {
        public Guid KeywordId { get; set; }       // UUID now, matches target_id in follows
        public string Title { get; set; } = string.Empty;
        public string? Body { get; set; }
        public Guid? RelatedPaperId { get; set; }
    }
}
