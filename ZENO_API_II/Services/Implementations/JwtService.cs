using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ZENO_API_II.Data;
using ZENO_API_II.Models;
using ZENO_API_II.Services.Interfaces;

namespace ZENO_API_II.Services.Implementations
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;
        private readonly ZenoDbContext _db;

        public JwtService(IConfiguration config, ZenoDbContext db)
        {
            _config = config;
            _db = db;
        }

        public string GenerateToken(UserLocal user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_config["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key not configured"));

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim("userId", user.Id.ToString()),
                new Claim("email", user.Email),
                new Claim("name", user.Name)
            };

            // Add OAuth claims if applicable
            if (user.IsOAuthUser)
            {
                claims.Add(new Claim("oauth_provider", user.OAuthProvider!));
                claims.Add(new Claim("oauth_subject_id", user.OAuthSubjectId!));
            }

            // Add credential version fingerprint for local-password accounts
            if (user.IsLocalUser && !string.IsNullOrWhiteSpace(user.PasswordSalt))
            {
                var credVersion = ComputeCredentialVersion(user.PasswordSalt!);
                claims.Add(new Claim("cred_version", credVersion));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1), // 1 hour
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public bool ValidateToken(string token, out Guid? userId)
        {
            userId = null;
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_config["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key not configured"));

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _config["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _config["Jwt:Audience"],
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "userId")?.Value;
                var credVersionClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "cred_version")?.Value;
                
                if (Guid.TryParse(userIdClaim, out var id))
                {
                    userId = id;

                    // If token has a credential version, validate against current user state
                    if (!string.IsNullOrEmpty(credVersionClaim))
                    {
                        var user = _db.Users.Find(id);
                        if (user == null)
                        {
                            return false;
                        }

                        if (string.IsNullOrWhiteSpace(user.PasswordSalt))
                        {
                            // If user no longer has local creds, then any token with cred_version becomes invalid
                            return false;
                        }

                        var expected = ComputeCredentialVersion(user.PasswordSalt!);
                        return string.Equals(credVersionClaim, expected, StringComparison.Ordinal);
                    }

                    // Tokens without cred_version are considered valid (e.g., OAuth-only tokens)
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private string ComputeCredentialVersion(string passwordSalt)
        {
            // Derive a fingerprint from server secret and current password salt.
            // This avoids exposing raw salt while staying stable until password changes.
            var secret = _config["Jwt:SecretKey"] ?? string.Empty;
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(passwordSalt));
            return Convert.ToHexString(bytes);
        }

        public async Task<UserLocal?> GetUserFromTokenAsync(string token)
        {
            if (!ValidateToken(token, out var userId))
                return null;

            if (userId == null)
                return null;

            return await _db.Users.FindAsync(userId.Value);
        }
    }
} 