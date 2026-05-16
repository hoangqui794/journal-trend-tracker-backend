using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;

namespace UserService.Models
{
    // ── ENUMS (matching PostgreSQL ENUMs) ─────────────────────
    public enum BookmarkEntity
    {
        paper,
        keyword,
        journal
    }

    public enum FollowType
    {
        keyword,
        journal,
        topic
    }

    public enum NotificationType
    {
        new_paper,
        trend_alert,
        system
    }

    public enum DeliveryStatus
    {
        pending,
        sent,
        failed
    }

    // ── USER PROFILES ─────────────────────────────────────────
    [Table("user_profiles")]
    public class UserProfile
    {
        [Key]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("bio")]
        public string? Bio { get; set; }

        [Column("institution")]
        public string? Institution { get; set; }

        [Column("research_fields")]
        public string[]? ResearchFields { get; set; }

        [Column("website_url")]
        public string? WebsiteUrl { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    // ── BOOKMARKS ─────────────────────────────────────────────
    [Table("bookmarks")]
    public class Bookmark
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("entity_type")]
        public BookmarkEntity EntityType { get; set; }

        [Column("entity_id")]
        public Guid EntityId { get; set; }

        [Column("entity_title")]
        public string? EntityTitle { get; set; }

        [Column("note")]
        public string? Note { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // ── FOLLOWS ───────────────────────────────────────────────
    [Table("follows")]
    public class Follow
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("follow_type")]
        public FollowType FollowType { get; set; }

        [Column("target_id")]
        public Guid TargetId { get; set; }

        [Column("target_name")]
        public string? TargetName { get; set; }

        [Column("notify_email")]
        public bool NotifyEmail { get; set; } = true;

        [Column("notify_inapp")]
        public bool NotifyInapp { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // ── NOTIFICATIONS ─────────────────────────────────────────
    [Table("notifications")]
    public class Notification
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("type")]
        public NotificationType Type { get; set; }

        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("body")]
        public string? Body { get; set; }

        [Column("related_id")]
        public Guid? RelatedId { get; set; }

        [Column("related_type")]
        public string? RelatedType { get; set; }

        [Column("is_read")]
        public bool IsRead { get; set; } = false;

        [Column("read_at")]
        public DateTime? ReadAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // ── EMAIL QUEUE ───────────────────────────────────────────
    [Table("email_queue")]
    public class EmailQueue
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("to_email")]
        public string ToEmail { get; set; } = string.Empty;

        [Column("subject")]
        public string Subject { get; set; } = string.Empty;

        [Column("body_html")]
        public string BodyHtml { get; set; } = string.Empty;

        [Column("status")]
        public DeliveryStatus Status { get; set; } = DeliveryStatus.pending;

        [Column("attempts")]
        public short Attempts { get; set; } = 0;

        [Column("last_attempted")]
        public DateTime? LastAttempted { get; set; }

        [Column("sent_at")]
        public DateTime? SentAt { get; set; }

        [Column("error_message")]
        public string? ErrorMessage { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
