using Microsoft.AspNetCore.Mvc;
using ZENO_API_II.DTOs.User;
using ZENO_API_II.Services.Interfaces;

namespace ZENO_API_II.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                var response = await _authService.RegisterAsync(registerDto);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var response = await _authService.LoginAsync(loginDto);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("logout")]
        public async Task<ActionResult<LogoutResponseDto>> Logout([FromBody] LogoutDto logoutDto)
        {
            try
            {
                var success = await _authService.LogoutAsync(logoutDto.Token);
                
                if (success)
                {
                    return Ok(new LogoutResponseDto 
                    { 
                        Success = true, 
                        Message = "Successfully logged out" 
                    });
                }
                else
                {
                    return BadRequest(new LogoutResponseDto 
                    { 
                        Success = false, 
                        Message = "Invalid token or logout failed" 
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new LogoutResponseDto 
                { 
                    Success = false, 
                    Message = "Internal server error" 
                });
            }
        }

        [HttpPost("oauth/google")]
        public async Task<ActionResult<AuthResponseDto>> GoogleLogin([FromBody] OAuthLoginDto oauthDto)
        {
            try
            {
                oauthDto.Provider = "Google";
                var response = await _authService.OAuthLoginAsync(oauthDto);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (NotImplementedException ex)
            {
                return StatusCode(501, new { message = "OAuth provider not implemented yet" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("oauth/microsoft")]
        public async Task<ActionResult<AuthResponseDto>> MicrosoftLogin([FromBody] OAuthLoginDto oauthDto)
        {
            try
            {
                oauthDto.Provider = "Microsoft";
                var response = await _authService.OAuthLoginAsync(oauthDto);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (NotImplementedException ex)
            {
                return StatusCode(501, new { message = "OAuth provider not implemented yet" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenDto refreshDto)
        {
            try
            {
                var response = await _authService.RefreshTokenAsync(refreshDto.RefreshToken);
                return Ok(response);
            }
            catch (NotImplementedException ex)
            {
                return StatusCode(501, new { message = "Refresh token functionality not implemented yet" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("validate")]
        public async Task<ActionResult<bool>> ValidateToken([FromBody] ValidateTokenDto validateDto)
        {
            try
            {
                var isValid = await _authService.ValidateTokenAsync(validateDto.Token);
                return Ok(new { isValid });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("revoke")]
        public async Task<ActionResult<bool>> RevokeToken([FromBody] RevokeTokenDto revokeDto)
        {
            try
            {
                var success = await _authService.RevokeTokenAsync(revokeDto.Token);
                return Ok(new { success });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }

    public class RefreshTokenDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class ValidateTokenDto
    {
        public string Token { get; set; } = string.Empty;
    }

    public class RevokeTokenDto
    {
        public string Token { get; set; } = string.Empty;
    }

    public class LogoutDto
    {
        public string Token { get; set; } = string.Empty;
    }

    public class LogoutResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
} 