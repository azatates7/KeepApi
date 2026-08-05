using KeepApi.Application.Interfaces;
using KeepApi.Application.Models.Common.Auth;
using KeepApi.Application.Models.Request.Auth;
using KeepApi.Application.Models.Response.Auth;
using KeepApi.Data.Entity;
using KeepApi.Infrastructure.Authentication.Jwt;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace KeepApi.Infrastructure.Authentication.Services
{
    public sealed class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtService _jwtService;
        private readonly JwtSettings _jwtSettings;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IJwtService jwtService,
            IOptions<JwtSettings> jwtOptions)
        {
            _userManager = userManager;
            _jwtService = jwtService;
            _jwtSettings = jwtOptions.Value;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByNameAsync(request.UserNameOrEmail)
                ?? await _userManager.FindByEmailAsync(request.UserNameOrEmail);

            if (user is null || user.IsDeleted)
            {
                throw new UnauthorizedAccessException("Kullanıcı adı/e-posta veya şifre hatalı.");
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                throw new UnauthorizedAccessException("Hesap kilitli. Lütfen daha sonra tekrar deneyin.");
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);

            if (!passwordValid)
            {
                await _userManager.AccessFailedAsync(user);
                throw new UnauthorizedAccessException("Kullanıcı adı/e-posta veya şifre hatalı.");
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var token = await _jwtService.GenerateTokenAsync(user);

            return new LoginResponse
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                Roles = roles,
                Token = token,
                ExpiresAt = DateTime.Now.AddMinutes(_jwtSettings.ExpireMinutes)
            };
        }

        public async Task RegisterAsync(RegisterRequest request)
        {
            if (request.Password != request.ConfirmPassword)
            {
                throw new InvalidOperationException("Şifreler eşleşmiyor.");
            }

            var user = new ApplicationUser
            {
                UserName = request.UserName,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                CreatedAt = DateTime.Now,
                IsDeleted = false,
                Status = 1
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(" ", result.Errors.Select(e => e.Description)));
            }

            if (!await _userManager.IsInRoleAsync(user, "User"))
            {
                await _userManager.AddToRoleAsync(user, "User");
            }
        }

        public async Task<UserDto> MeAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString())
                ?? throw new KeyNotFoundException("Kullanıcı bulunamadı.");

            var roles = await _userManager.GetRolesAsync(user);

            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles
            };
        }
    }
}