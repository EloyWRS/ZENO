using ZENO_API_II.DTOs.User;

namespace ZENO_API_II.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> OAuthLoginAsync(OAuthLoginDto oauthDto);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        Task<bool> ValidateTokenAsync(string token);
        Task<bool> RevokeTokenAsync(string token);
        Task<bool> LogoutAsync(string token);
        Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    }
} 