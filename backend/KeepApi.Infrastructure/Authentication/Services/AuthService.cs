using KeepApi.Application.Interfaces;
using KeepApi.Application.Models.Common.Auth;
using KeepApi.Application.Models.Request.Auth;
using KeepApi.Application.Models.Response.Auth;
using KeepApi.Data.Entity;
using KeepApi.Infrastructure.Authentication.Jwt;
using KeepApi.Infrastructure.Authentication.PasswordReset;
using KeepApi.Infrastructure.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace KeepApi.Infrastructure.Authentication.Services
{
    public sealed class AuthService : IAuthService
    {
        private static readonly TimeSpan ResetCodeTtl = TimeSpan.FromMinutes(10);

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtService _jwtService;
        private readonly JwtSettings _jwtSettings;
        private readonly IPasswordResetCodeStore _resetCodeStore;
        private readonly IEmailService _emailService;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IJwtService jwtService,
            IOptions<JwtSettings> jwtOptions,
            IPasswordResetCodeStore resetCodeStore,
            IEmailService emailService)
        {
            _userManager = userManager;
            _jwtService = jwtService;
            _jwtSettings = jwtOptions.Value;
            _resetCodeStore = resetCodeStore;
            _emailService = emailService;
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

            if (!user.EmailConfirmed)
            {
                throw new UnauthorizedAccessException(
                    "E-posta adresiniz henüz doğrulanmadı. Lütfen e-postanıza gönderilen kodu girin.");
            }

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
            var user1 = await _userManager.FindByNameAsync(request.UserName.Trim());
            var user2 = await _userManager.FindByEmailAsync(request.Email.Trim());

            if (user1 is not null || user2 is not null)
            {
                throw new InvalidOperationException("Kullanıcı adı/e-posta zaten kayıtlı.");
            }

            if (request.Password != request.ConfirmPassword)
            {
                throw new InvalidOperationException("Şifreler eşleşmiyor.");
            }

            var user = new ApplicationUser
            {
                UserName = request.UserName,
                Email = request.Email,
                EmailConfirmed = false,
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

            var code = _resetCodeStore.GenerateCode(user.Id, ResetCodeTtl);

            await _emailService.SendAsync(
                user.Email!,
                "Keep Todo - Hesap Doğrulama Kodu",
                $"Merhaba {user.FirstName},\n\n" +
                $"Hesabınızı doğrulamak için kodunuz: {code}\n" +
                $"Bu kod {ResetCodeTtl.TotalMinutes:0} dakika geçerlidir.\n\n" +
                "Bu kaydı siz oluşturmadıysanız bu e-postayı yok sayabilirsiniz.");
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

        public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            // Kayıtlı olmayan bir e-posta için de sessizce başarılı dönüyoruz;
            // aksi halde "bu e-posta sistemde var mı yok mu" bilgisini dışarı sızdırırız.
            if (user is null || user.IsDeleted)
            {
                return;
            }

            var code = _resetCodeStore.GenerateCode(user.Id, ResetCodeTtl);

            await _emailService.SendAsync(
                user.Email!,
                "Keep Todo - Şifre Sıfırlama Kodu",
                $"Merhaba {user.FirstName},\n\n" +
                $"Şifre sıfırlama kodunuz: {code}\n" +
                $"Bu kod {ResetCodeTtl.TotalMinutes:0} dakika geçerlidir.\n\n" +
                "Bu isteği siz yapmadıysanız bu e-postayı yok sayabilirsiniz.");
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            if (request.NewPassword != request.ConfirmNewPassword)
            {
                throw new InvalidOperationException("Şifreler eşleşmiyor.");
            }

            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user is null || user.IsDeleted)
            {
                throw new InvalidOperationException("Kod veya e-posta hatalı.");
            }

            if (!_resetCodeStore.TryValidateAndConsume(user.Id, request.Code))
            {
                throw new InvalidOperationException("Kod hatalı veya süresi dolmuş.");
            }

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(" ", result.Errors.Select(e => e.Description)));
            }
        }

        public async Task VerifyEmailAsync(VerifyEmailRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user is null || user.IsDeleted)
            {
                throw new InvalidOperationException("Kod veya e-posta hatalı.");
            }

            if (user.EmailConfirmed)
            {
                return; // zaten doğrulanmış, sessizce başarı say
            }

            if (!_resetCodeStore.TryValidateAndConsume(user.Id, request.Code))
            {
                throw new InvalidOperationException("Kod hatalı veya süresi dolmuş.");
            }

            user.EmailConfirmed = true;
            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(" ", updateResult.Errors.Select(e => e.Description)));
            }
        }
    }
}