using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ZENO_API_II.Data;
using ZENO_API_II.DTOs.User;
using ZENO_API_II.Models;
using ZENO_API_II.Services.Interfaces;

namespace ZENO_API_II.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly ZenoDbContext _db;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _config;
        private readonly IUserSetupService _userSetupService;

        public AuthService(ZenoDbContext db, IJwtService jwtService, IConfiguration config, IUserSetupService userSetupService)
        {
            _db = db;
            _jwtService = jwtService;
            _config = config;
            _userSetupService = userSetupService;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            // Check if user already exists
            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
            if (existingUser != null)
                throw new InvalidOperationException("User with this email already exists");

            // Create password hash
            var (passwordHash, passwordSalt) = HashPassword(registerDto.Password);

            // Create new user
            var user = new UserLocal
            {
                Id = Guid.NewGuid(),
                Name = registerDto.Name,
                Email = registerDto.Email,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Automatically create assistant and thread for the new user
            try
            {
                await _userSetupService.SetupNewUserAsync(user);
            }
            catch (Exception ex)
            {
                // Log the error but don't fail registration
                // The user can still register, assistant/thread will be created on first login
                Console.WriteLine($"Failed to setup user assistant: {ex.Message}");
            }

            // Generate tokens
            var token = _jwtService.GenerateToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = new ReadUserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Language = user.Language,
                    Credits = user.Credits,
                    CreatedAt = user.CreatedAt
                }
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
            if (user == null)
                throw new InvalidOperationException("Invalid email or password");

            // Verify password
            if (!VerifyPassword(loginDto.Password, user.PasswordHash!, user.PasswordSalt!))
                throw new InvalidOperationException("Invalid email or password");

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Generate tokens
            var token = _jwtService.GenerateToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = new ReadUserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Language = user.Language,
                    Credits = user.Credits,
                    CreatedAt = user.CreatedAt
                }
            };
        }

        public async Task<AuthResponseDto> OAuthLoginAsync(OAuthLoginDto oauthDto)
        {
            // Validate OAuth token with provider
            var userInfo = await ValidateOAuthTokenAsync(oauthDto);
            
            // Find or create user
            var user = await _db.Users.FirstOrDefaultAsync(u => 
                u.OAuthProvider == oauthDto.Provider && 
                u.OAuthSubjectId == userInfo.SubjectId);

            bool isNewUser = false;
            if (user == null)
            {
                // Create new OAuth user
                user = new UserLocal
                {
                    Id = Guid.NewGuid(),
                    Name = userInfo.Name,
                    Email = userInfo.Email,
                    OAuthProvider = oauthDto.Provider,
                    OAuthSubjectId = userInfo.SubjectId,
                    OAuthRefreshToken = oauthDto.RefreshToken,
                    OAuthTokenExpiresAt = oauthDto.TokenExpiresAt,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Users.Add(user);
                isNewUser = true;
            }
            else
            {
                // Update existing OAuth user
                user.Name = userInfo.Name;
                user.Email = userInfo.Email;
                user.OAuthRefreshToken = oauthDto.RefreshToken;
                user.OAuthTokenExpiresAt = oauthDto.TokenExpiresAt;
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Automatically create assistant and thread for new OAuth users
            if (isNewUser)
            {
                try
                {
                    await _userSetupService.SetupNewUserAsync(user);
                }
                catch (Exception ex)
                {
                    // Log the error but don't fail login
                    // The user can still login, assistant/thread will be created on first access
                    Console.WriteLine($"Failed to setup OAuth user assistant: {ex.Message}");
                }
            }

            // Generate tokens
            var token = _jwtService.GenerateToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = new ReadUserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Language = user.Language,
                    Credits = user.Credits,
                    CreatedAt = user.CreatedAt
                }
            };
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            // In a real implementation, you'd validate the refresh token against a database
            // For now, we'll just generate a new token (simplified)
            throw new NotImplementedException("Refresh token functionality not implemented yet");
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            return _jwtService.ValidateToken(token, out _);
        }

        public async Task<bool> RevokeTokenAsync(string token)
        {
            // In a real implementation, you'd add the token to a blacklist
            // For now, we'll just return true (simplified)
            return true;
        }

        public async Task<bool> LogoutAsync(string token)
        {
            try
            {
                // Validate the token first
                if (!_jwtService.ValidateToken(token, out var userId))
                {
                    return false; // Invalid token
                }

                // In a real implementation, you would:
                // 1. Add the token to a blacklist/revoked tokens table
                // 2. Invalidate any refresh tokens associated with this user
                // 3. Clear any server-side sessions

                // For now, we'll just return true (simplified implementation)
                // In production, you'd want to store revoked tokens in a database
                // and check against that list when validating tokens

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private (string hash, string salt) HashPassword(string password)
        {
            using var hmac = new HMACSHA512();
            var salt = Convert.ToBase64String(hmac.Key);
            var hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
            return (hash, salt);
        }

        private bool VerifyPassword(string password, string hash, string salt)
        {
            using var hmac = new HMACSHA512(Convert.FromBase64String(salt));
            var computedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
            return hash == computedHash;
        }

        private async Task<OAuthUserInfo> ValidateOAuthTokenAsync(OAuthLoginDto oauthDto)
        {
            // This is a simplified implementation
            // In a real app, you'd validate the token with Google/Microsoft APIs
            switch (oauthDto.Provider.ToLower())
            {
                case "google":
                    return await ValidateGoogleTokenAsync(oauthDto.AccessToken);
                case "microsoft":
                    return await ValidateMicrosoftTokenAsync(oauthDto.AccessToken);
                default:
                    throw new InvalidOperationException($"Unsupported OAuth provider: {oauthDto.Provider}");
            }
        }

        private async Task<OAuthUserInfo> ValidateGoogleTokenAsync(string accessToken)
        {
            // Simplified - in real implementation, validate with Google API
            // For now, we'll assume the token is valid and extract user info
            throw new NotImplementedException("Google OAuth validation not implemented yet");
        }

        private async Task<OAuthUserInfo> ValidateMicrosoftTokenAsync(string accessToken)
        {
            // Simplified - in real implementation, validate with Microsoft API
            // For now, we'll assume the token is valid and extract user info
            throw new NotImplementedException("Microsoft OAuth validation not implemented yet");
        }
    }

    public class OAuthUserInfo
    {
        public string SubjectId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
} 