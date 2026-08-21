namespace KeepApi.Infrastructure.Notifications
{
    public interface ILoginFailureNotifier
    {
        /// <summary>Sabit bir bildirim adresine ("Notifications:LoginFailureEmail" DB ayarı)
        /// başarısız bir giriş denemesi hakkında e-posta gönderir. Hangfire fire-and-forget
        /// job'ı olarak çalıştırılmak üzere tasarlandı — login isteğini SMTP'nin süresine
        /// bağlamamak için AuthService bunu senkron beklemez, sadece kuyruğa atar.</summary>
        Task NotifyAsync(string userNameOrEmail, string reason, DateTime occurredAt);
    }
}