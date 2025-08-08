using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZENO_API_II.Data;
using ZENO_API_II.DTOs.User;
using ZENO_API_II.Models;
using ZENO_API_II.Services.Interfaces;

namespace ZENO_API_II.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ZenoDbContext _db;
        private readonly IJwtService _jwtService;
        private readonly IAuthService _authService;

        public UserController(ZenoDbContext db, IJwtService jwtService, IAuthService authService)
        {
            _db = db;
            _jwtService = jwtService;
            _authService = authService;
        }

        [HttpGet("me")]
        public async Task<ActionResult<ReadUserDto>> GetCurrentUser()
        {
            try
            {
                // Extract token from Authorization header
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { message = "No valid authorization header" });
                }

                var token = authHeader.Substring("Bearer ".Length);
                var user = await _jwtService.GetUserFromTokenAsync(token);

                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var userDto = new ReadUserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Language = user.Language,
                    Credits = user.Credits,
                    CreatedAt = user.CreatedAt
                };

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ReadUserDto>> GetUserById(Guid id)
        {
            var user = await _db.Users.FindAsync(id);

            if (user == null)
                return NotFound("User not found");

            var result = new ReadUserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Language = user.Language,
                Credits = user.Credits,
                CreatedAt = user.CreatedAt
            };

            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ReadUserDto>> UpdateUser(Guid id, [FromBody] UpdateUserDto updateDto)
        {
            var user = await _db.Users.FindAsync(id);

            if (user == null)
                return NotFound("User not found");

            // Update allowed fields
            if (!string.IsNullOrEmpty(updateDto.Name))
                user.Name = updateDto.Name;

            if (!string.IsNullOrEmpty(updateDto.Language))
                user.Language = updateDto.Language;

            await _db.SaveChangesAsync();

            var result = new ReadUserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Language = user.Language,
                Credits = user.Credits,
                CreatedAt = user.CreatedAt
            };

            return Ok(result);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReadUserDto>>> GetAllUsers()
        {
            var users = await _db.Users
                .Select(u => new ReadUserDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    Language = u.Language,
                    Credits = u.Credits,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost("change-password")]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { message = "No valid authorization header" });
                }

                var token = authHeader.Substring("Bearer ".Length);
                if (!_jwtService.ValidateToken(token, out var userId) || userId == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var changed = await _authService.ChangePasswordAsync(userId.Value, dto.CurrentPassword, dto.NewPassword);
                if (!changed)
                {
                    return BadRequest(new { message = "Current password is incorrect" });
                }

                return Ok(new { message = "Password updated successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
