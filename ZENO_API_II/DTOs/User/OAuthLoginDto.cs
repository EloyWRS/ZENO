using System.ComponentModel.DataAnnotations;

namespace ZENO_API_II.DTOs.User
{
    public class OAuthLoginDto
    {
        [Required]
        public string Provider { get; set; } // "Google", "Microsoft"

        [Required]
        public string AccessToken { get; set; }

        public string? RefreshToken { get; set; }
        public DateTime? TokenExpiresAt { get; set; }
    }
} 