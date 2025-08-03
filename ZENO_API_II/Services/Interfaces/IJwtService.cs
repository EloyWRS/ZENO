using ZENO_API_II.Models;

namespace ZENO_API_II.Services.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(UserLocal user);
        string GenerateRefreshToken();
        bool ValidateToken(string token, out Guid? userId);
        Task<UserLocal?> GetUserFromTokenAsync(string token);
    }
} 