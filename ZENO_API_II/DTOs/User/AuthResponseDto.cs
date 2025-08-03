namespace ZENO_API_II.DTOs.User
{
    public class AuthResponseDto
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public ReadUserDto User { get; set; }
    }
} 