using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZeNO_API_II.DTOs.User;
using ZENO_API_II.Data;
using ZENO_API_II.DTOs.User;
using ZENO_API_II.Models;

namespace ZENO_API_II.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ZenoDbContext _db;

        public UserController(ZenoDbContext db)
        {
            _db = db;
        }

        // GET /api/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReadUserDto>>> GetAllUsers()
        {
            var users = await _db.Users.ToListAsync();

            var userDtos = users.Select(u => new ReadUserDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Language = u.Language,
                CreatedAt = u.CreatedAt
            });

            return Ok(userDtos);
        }

        // GET /api/users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ReadUserDto>> GetUserById(Guid id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            var dto = new ReadUserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Language = user.Language,
                CreatedAt = user.CreatedAt
            };

            return Ok(dto);
        }

        // POST /api/users
        [HttpPost]
        public async Task<ActionResult<ReadUserDto>> CreateUser([FromBody] CreateUserDto dto)
        {
            var user = new UserLocal
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Email = dto.Email,
                Language = dto.Language,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var result = new ReadUserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Language = user.Language,
                CreatedAt = user.CreatedAt
            };

            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, result);
        }

        // PATCH /api/users/{id}
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.Name = dto.Name ?? user.Name;
            user.Email = dto.Email ?? user.Email;
            user.Language = dto.Language ?? user.Language;

            await _db.SaveChangesAsync();

            return NoContent();
        }

        // DELETE /api/users/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }

}
