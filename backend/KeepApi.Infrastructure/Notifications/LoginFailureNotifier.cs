using Hangfire;
using KeepApi.Infrastructure.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KeepApi.Infrastructure.Notifications
{
    public sealed class LoginFailureNotifier : ILoginFailureNotifier
    {
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoginFailureNotifier> _logger;

        public LoginFailureNotifier(
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<LoginFailureNotifier> logger)
        {
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        ///TODO Hangfire Prod Panel kontrol edilmeli. Hata fırlatan job'lar tekrar deneniyor mu, yoksa Failed listesinde mi kalıyor?
        // Hangfire, hata fırlatan bir job'ı otomatik olarak tekrar dener (varsayılan 10 kez). Tekrar için denetme kapatıldı; kalıcı bir hata olursa Dashboard'da "Failed" listesinde görünür.
        [AutomaticRetry(Attempts = 0)]
        public async Task NotifyAsync(string userNameOrEmail, string reason, DateTime occurredAt)
        {
            // "Sabit" bildirim adresi — kod içine gömülmek yerine, projenin geri kalanıyla
            // tutarlı olması için DB-backed AppSettings'ten okunuyor (bkz. AppSettingsSeeder).
            var notifyAddress = _configuration["Notifications:LoginFailureEmail"];

            if (string.IsNullOrWhiteSpace(notifyAddress))
            {
                _logger.LogWarning(
                    "Notifications:LoginFailureEmail ayarlanmamış; başarısız giriş bildirimi atlandı ({UserNameOrEmail}).",
                    userNameOrEmail);
                return;
            }

            await _emailService.SendAsync(
                notifyAddress,
                "KeepApi — Başarısız Giriş Denemesi",
                $"Kullanıcı adı/e-posta: {userNameOrEmail}\n" +
                $"Sebep: {reason}\n" +
                $"Zaman: {occurredAt:dd.MM.yyyy HH:mm:ss}",
                CancellationToken.None);
        }
    }
}