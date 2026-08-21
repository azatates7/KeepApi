using KeepApi.Application.Interfaces;
using KeepApi.Application.Models.Common.Auth;
using KeepApi.Application.Models.Request.Auth;
using KeepApi.Application.Models.Response.Auth;
using KeepApi.Data.Entity;
using KeepApi.Infrastructure.Authentication.External;
using KeepApi.Infrastructure.Authentication.Jwt;
using KeepApi.Infrastructure.Authentication.PasswordReset;
using KeepApi.Infrastructure.Authentication.RefreshTokens;
using KeepApi.Infrastructure.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KeepApi.Infrastructure.Authentication.Services
{
    public sealed class AuthService : IAuthService
    {
        private static readonly TimeSpan ResetCodeTtl = TimeSpan.FromMinutes(10);

        // İki kademeli hesap kilitleme: her 3 başarısız denemede bir 5 dk geçici kilit,
        // toplam 10 başarısız denemede kalıcı kilit (şifre sıfırlama/değişimiyle açılır).
        // AccessFailedCount/LockoutEnd, ASP.NET Identity'nin AspNetUsers tablosunda
        // zaten DB'de tutuluyor — ayrı bir kolon/tablo gerekmiyor.
        private const int TempLockEveryNFailures = 3;
        private static readonly TimeSpan TempLockDuration = TimeSpan.FromMinutes(5);
        private const int PermanentLockThreshold = 10;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtService _jwtService;
        private readonly JwtSettings _jwtSettings;
        private readonly IPasswordResetCodeStore _resetCodeStore;
        private readonly IEmailService _emailService;
        private readonly IExternalOAuthClient _externalOAuthClient;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IJwtService jwtService,
            IOptions<JwtSettings> jwtOptions,
            IPasswordResetCodeStore resetCodeStore,
            IEmailService emailService,
            IExternalOAuthClient externalOAuthClient,
            IRefreshTokenService refreshTokenService,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _jwtService = jwtService;
            _jwtSettings = jwtOptions.Value;
            _resetCodeStore = resetCodeStore;
            _emailService = emailService;
            _externalOAuthClient = externalOAuthClient;
            _refreshTokenService = refreshTokenService;
            _logger = logger;
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
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                if (lockoutEnd == DateTimeOffset.MaxValue)
                {
                    throw new UnauthorizedAccessException(
                        "Hesap çok sayıda hatalı deneme nedeniyle kilitlendi. Şifrenizi sıfırlamanız gerekiyor.");
                }

                throw new UnauthorizedAccessException("Hesap kilitli. Lütfen daha sonra tekrar deneyin.");
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);

            if (!passwordValid)
            {
                await RegisterFailedLoginAttemptAsync(user);
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
            var (refreshToken, refreshExpiresAt) = await _refreshTokenService.IssueAsync(user.Id);

            return new LoginResponse
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                Roles = roles,
                Token = token,
                ExpiresAt = DateTime.Now.AddMinutes(_jwtSettings.ExpireMinutes),
                RefreshToken = refreshToken,
                RefreshTokenExpiresAt = refreshExpiresAt
            };
        }

        /// <summary>
        /// Başarısız girişi DB'ye (AspNetUsers.AccessFailedCount) işler ve iki kademeli
        /// kilitleme uygular: her 3 denemede bir 5 dk geçici kilit, toplamda 10 denemede
        /// kalıcı kilit (DateTimeOffset.MaxValue) — bu sadece ResetPasswordAsync (veya
        /// ileride eklenecek bir şifre değiştirme akışı) ile kaldırılır.
        /// Identity'nin varsayılan AccessFailedAsync'i kilitlendiğinde sayacı sıfırladığı
        /// için burada bilerek kullanılmadı — 10'a kadar kümülatif sayım gerekiyor.
        /// </summary>
        private async Task RegisterFailedLoginAttemptAsync(ApplicationUser user)
        {
            // Increment the failed count via AccessFailedAsync, then read the current count.
            var accessFailedResult = await _userManager.AccessFailedAsync(user);
            if (!accessFailedResult.Succeeded)
            {
                _logger.LogWarning("AccessFailedAsync failed for user {UserId}.", user.Id);
            }

            var failedCount = await _userManager.GetAccessFailedCountAsync(user);

            if (failedCount >= PermanentLockThreshold)
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                _logger.LogWarning("Kullanıcı {UserId} {FailedCount} başarısız denemeden sonra kalıcı olarak kilitlendi.", user.Id, failedCount);
            }
            else if (failedCount % TempLockEveryNFailures == 0)
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.Add(TempLockDuration));
                _logger.LogInformation("Kullanıcı {UserId} {FailedCount} başarısız denemeden sonra {Minutes} dk kilitlendi.", user.Id, failedCount, TempLockDuration.TotalMinutes);
            }
        }

        public async Task<LoginResponse> ExternalLoginAsync(string provider, ExternalLoginRequest request)
        {
            var providerKeyNormalized = provider.Trim().ToLowerInvariant();

            ExternalUserInfo externalUser;
            try
            {
                externalUser = await _externalOAuthClient.ExchangeAndGetUserInfoAsync(
                    providerKeyNormalized, request.Code, request.RedirectUri);
            }
            catch (ExternalAuthException ex)
            {
                throw new UnauthorizedAccessException(ex.Message);
            }

            // 1) Daha önce bu sağlayıcı ile giriş yapmış mı? (AspNetUserLogins)
            var user = await _userManager.FindByLoginAsync(providerKeyNormalized, externalUser.ProviderKey);

            if (user is null)
            {
                // 2) Aynı e-posta ile normal kayıtlı bir hesap var mı? Varsa hesabı bu sağlayıcıya bağla.
                user = await _userManager.FindByEmailAsync(externalUser.Email);

                if (user is not null && user.IsDeleted)
                {
                    throw new UnauthorizedAccessException("Bu hesap kullanılamıyor.");
                }

                if (user is null)
                {
                    // 3) Hiç yoksa yeni kullanıcı oluştur. Sağlayıcı e-postayı doğruladıysa
                    // bizim de tekrar e-posta doğrulaması istememize gerek yok.
                    user = new ApplicationUser
                    {
                        UserName = await GenerateUniqueUserNameAsync(externalUser),
                        Email = externalUser.Email,
                        EmailConfirmed = externalUser.EmailVerified,
                        FirstName = string.IsNullOrWhiteSpace(externalUser.FirstName) ? providerKeyNormalized : externalUser.FirstName,
                        LastName = externalUser.LastName,
                        CreatedAt = DateTime.Now,
                        IsDeleted = false,
                        Status = 1
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        throw new UnauthorizedAccessException(
                            string.Join(" ", createResult.Errors.Select(e => e.Description)));
                    }

                    if (!await _userManager.IsInRoleAsync(user, "User"))
                    {
                        await _userManager.AddToRoleAsync(user, "User");
                    }
                }

                var addLoginResult = await _userManager.AddLoginAsync(
                    user,
                    new UserLoginInfo(providerKeyNormalized, externalUser.ProviderKey, provider));

                if (!addLoginResult.Succeeded)
                {
                    throw new UnauthorizedAccessException(
                        string.Join(" ", addLoginResult.Errors.Select(e => e.Description)));
                }
            }

            if (user.IsDeleted)
            {
                throw new UnauthorizedAccessException("Bu hesap kullanılamıyor.");
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                throw new UnauthorizedAccessException("Hesap kilitli. Lütfen daha sonra tekrar deneyin.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = await _jwtService.GenerateTokenAsync(user);
            var (refreshToken, refreshExpiresAt) = await _refreshTokenService.IssueAsync(user.Id);

            return new LoginResponse
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                Roles = roles,
                Token = token,
                ExpiresAt = DateTime.Now.AddMinutes(_jwtSettings.ExpireMinutes),
                RefreshToken = refreshToken,
                RefreshTokenExpiresAt = refreshExpiresAt
            };
        }

        private async Task<string> GenerateUniqueUserNameAsync(ExternalUserInfo externalUser)
        {
            var baseName = externalUser.Email.Contains('@')
                ? externalUser.Email[..externalUser.Email.IndexOf('@')]
                : $"{externalUser.FirstName}{externalUser.LastName}";

            baseName = new string(baseName.Where(char.IsLetterOrDigit).ToArray());
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = "user";
            }

            var candidate = baseName;
            var suffix = 0;

            while (await _userManager.FindByNameAsync(candidate) is not null)
            {
                suffix++;
                candidate = $"{baseName}{suffix}";
            }

            return candidate;
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

            var passwordValidationResult = await CheckPasswordIsValid(request.Password);
            if (!string.IsNullOrWhiteSpace(passwordValidationResult))
            {
                throw new InvalidOperationException(passwordValidationResult);
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

        private static async Task<string> CheckPasswordIsValid(string password)
        {
            var resultString = string.Empty;
            if (string.IsNullOrWhiteSpace(password))
            {
                resultString = "Şifre boş olamaz.";
            }
            else if (password.Length < 8)
            {
                resultString = "Şifre en az 8 karakter olmalıdır.";
            }
            else if (password.Length > 30)
            {
                resultString = "Şifre en fazla 30 karakter olmalıdır.";
            }
            else if (!password.Any(char.IsUpper))
            {
                resultString = "Şifre en az bir büyük harf içermelidir.";
            }
            else if (!password.Any(char.IsLower))
            {
                resultString = "Şifre en az bir küçük harf içermelidir.";
            }
            else if (!password.Any(char.IsDigit))
            {
                resultString = "Şifre en az bir rakam içermelidir.";
            }
            else if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                resultString = "Şifre en az bir özel karakter içermelidir.";
            }

            return resultString;
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
                PreferredLanguage = user.PreferredLanguage,
                Roles = roles
            };
        }

        public async Task UpdateLanguageAsync(Guid userId, string language)
        {
            if (language != "tr" && language != "en")
            {
                throw new InvalidOperationException("Geçersiz dil. Desteklenen diller: tr, en.");
            }

            var user = await _userManager.FindByIdAsync(userId.ToString())
                ?? throw new KeyNotFoundException("Kullanıcı bulunamadı.");

            user.PreferredLanguage = language;
            await _userManager.UpdateAsync(user);
        }

        public async Task<LoginResponse> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new UnauthorizedAccessException("Refresh token gerekli.");
            }

            var rotated = await _refreshTokenService.ValidateAndRotateAsync(refreshToken);
            if (rotated is null)
            {
                throw new UnauthorizedAccessException("Refresh token geçersiz veya süresi dolmuş.");
            }

            var user = await _userManager.FindByIdAsync(rotated.Value.UserId.ToString());
            if (user is null || user.IsDeleted)
            {
                throw new UnauthorizedAccessException("Kullanıcı bulunamadı.");
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                throw new UnauthorizedAccessException("Hesap kilitli. Lütfen daha sonra tekrar deneyin.");
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
                ExpiresAt = DateTime.Now.AddMinutes(_jwtSettings.ExpireMinutes),
                RefreshToken = rotated.Value.Token,
                RefreshTokenExpiresAt = rotated.Value.ExpiresAt
            };
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await _refreshTokenService.RevokeAsync(refreshToken);
            }
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            // Kayıtlı olmayan bir e-posta için de sessizce başarılı dönüyoruz aksi halde "bu e-posta sistemde var mı yok mu" bilgisini dışarı sızdırırız.
            if (user is null || user.IsDeleted)
            {
                //throw new InvalidOperationException("Kullanıcı e-posta kayıtlı değil.");
                _logger.LogError($"Kullanıcı e-posta kayıt değil. {request.Email}");
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

            var passwordValidationResult = await CheckPasswordIsValid(request.NewPassword);
            if (!string.IsNullOrWhiteSpace(passwordValidationResult))
            {
                throw new InvalidOperationException(passwordValidationResult);
            }

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);

            if (result.Succeeded)
            {
                // Şifre sıfırlandığında hem geçici hem kalıcı kilit kaldırılır, sayaç sıfırlanır.
                await _userManager.SetLockoutEndDateAsync(user, null);
                await _userManager.ResetAccessFailedCountAsync(user);
            }

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