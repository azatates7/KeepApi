using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using KeepApi.Common.Models;
using KeepApi.Application.Models.Response.Auth;
using KeepApi.Application.Models.Request.Auth;
using KeepApi.Application.Interfaces;
using KeepApi.Application.Models.Common.Auth;

namespace KeepApi.Controller
{    
    /// <summary>
    /// Login, kayıt ve mevcut kullanıcı bilgisi için Identity/JWT tabanlı auth controller.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>Kullanıcı adı/e-posta ve şifre ile giriş yapar, JWT döner.</summary>
        /// <response code="200">Giriş başarılı, Bearer token döner.</response>
        /// <response code="401">Kullanıcı adı/e-posta veya şifre hatalı.</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                return Ok(ApiResponse<LoginResponse>.Ok(result, "Giriş başarılı."));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<LoginResponse>.Fail(ex.Message));
            }
        }

        /// <summary>Yeni kullanıcı kaydı oluşturur (varsayılan "User" rolü ile).</summary>
        /// <response code="200">Kayıt başarılı.</response>
        /// <response code="400">Kayıt bilgileri geçersiz.</response>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<object>>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                await _authService.RegisterAsync(request);
                return Ok(ApiResponse<object>.Ok(new { }, "Kayıt başarılı."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        /// <summary>Token sahibi kullanıcının bilgilerini döner. Bearer token gerektirir.</summary>
        /// <response code="200">Kullanıcı bilgisi döner.</response>
        /// <response code="401">Geçersiz veya eksik token.</response>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<UserDto>>> Me()
        {
            var userId = GetCurrentUserId();

            if (userId is null)
            {
                return Unauthorized(ApiResponse<UserDto>.Fail("Token içinde kullanıcı bilgisi bulunamadı."));
            }

            var user = await _authService.MeAsync(userId.Value);
            return Ok(ApiResponse<UserDto>.Ok(user));
        }

        private Guid? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }
}
