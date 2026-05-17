using System;

namespace IdentityService.Models
{
    public enum AuthProvider { email, google, github }
    public enum UserRole { researcher, lecturer, student, admin }
    public enum UserStatus { active, locked, pending }

    public class User
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PasswordHash { get; set; } // Null if OAuth
        public string? AvatarUrl { get; set; }
        public AuthProvider Provider { get; set; } = AuthProvider.email;
        public string? ProviderId { get; set; } // Google sub ID
        public UserRole Role { get; set; } = UserRole.student;
        public UserStatus Status { get; set; } = UserStatus.active;
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
