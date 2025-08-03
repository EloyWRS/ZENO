using OpenAI.Assistants;
using System.ComponentModel.DataAnnotations;

namespace ZENO_API_II.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class UserLocal
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        // Local Authentication (Email + Password)
        public string? PasswordHash { get; set; }
        public string? PasswordSalt { get; set; }

        // OAuth2 Authentication
        public string? OAuthProvider { get; set; } // "Google", "Microsoft", etc.
        public string? OAuthSubjectId { get; set; } // Unique ID from OAuth provider
        public string? OAuthRefreshToken { get; set; } // For accessing provider APIs
        public DateTime? OAuthTokenExpiresAt { get; set; }

        // User Preferences
        public string Language { get; set; } = "pt-PT";

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        // Navigation Properties
        public AssistantLocal Assistant { get; set; } // 1:1
        public int Credits { get; set; } = 0;
        public DateTime LastCreditUpdate { get; set; } = DateTime.UtcNow;

        public List<CreditTransaction> CreditTransactions { get; set; } = new();

        // Helper methods
        public bool IsOAuthUser => !string.IsNullOrEmpty(OAuthProvider) && !string.IsNullOrEmpty(OAuthSubjectId);
        public bool IsLocalUser => !string.IsNullOrEmpty(PasswordHash);
        public bool CanUseOAuth => !string.IsNullOrEmpty(OAuthProvider) && !string.IsNullOrEmpty(OAuthRefreshToken);
    }
}
