using KeepApi.Application.Models.Common.Auth;
using KeepApi.Application.Models.Request.Auth;
using KeepApi.Application.Models.Response.Auth;

namespace KeepApi.Application.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);

        /// <summary>Google/Microsoft/GitHub authorization code'unu doğrulayıp eşleşen (veya yeni oluşturulan) kullanıcı için JWT döner.</summary>
        
        Task<LoginResponse> ExternalLoginAsync(string provider, ExternalLoginRequest request);

        Task RegisterAsync(RegisterRequest request);

        Task<UserDto> MeAsync(Guid userId);

        /// <summary>Kullanıcı e-postaya kayıtlıysa, e-postasına 6 haneli bir sıfırlama kodu gönderir.
        /// E-posta kayıtlı değilse de sessizce başarılı döner (enumeration koruması).</summary>

        Task ForgotPasswordAsync(ForgotPasswordRequest request);

        /// <summary>Kodu doğrular ve doğruysa şifreyi yeniler.</summary>

        Task ResetPasswordAsync(ResetPasswordRequest request);

        /// <summary>Kayıt sırasında e-postaya gönderilen doğrulama kodunu kontrol eder, doğruysa hesabı (EmailConfirmed) aktifleştirir.</summary>

        Task VerifyEmailAsync(VerifyEmailRequest request);

    }
}
