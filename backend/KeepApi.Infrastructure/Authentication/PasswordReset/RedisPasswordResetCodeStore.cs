using Microsoft.EntityFrameworkCore.Storage;
using StackExchange.Redis;

namespace KeepApi.Infrastructure.Authentication.PasswordReset
{
    public interface IPasswordResetCodeStore
    {
        /// <summary>Kullanıcı için yeni bir doğrulama kodu üretir ve saklar (eskisinin yerine geçer).</summary>
        Task<string> GenerateCodeAsync(Guid userId, TimeSpan ttl);

        /// <summary>Kodu doğrular; doğruysa tüketir (bir daha kullanılamaz) ve true döner.</summary>
        Task<bool> TryValidateAndConsumeAsync(Guid userId, string code);
    }

    /// <summary>Redis-backed implementasyon — kodlar Redis'in kendi TTL mekanizmasıyla saklanır,
    /// süresi dolan anahtar Redis tarafından otomatik silinir (ayrıca "expired mi" kontrolüne
    /// gerek yok). Projede zaten çalışan Redis'i kullanır; backend restart olsa bile bekleyen
    /// kodlar kaybolmaz — InMemory implementasyonun (ConcurrentDictionary) aksine.</summary>
    public sealed class RedisPasswordResetCodeStore : IPasswordResetCodeStore
    {
        private const string KeyPrefix = "password-reset-code:";

        private readonly IDatabase _redis;

        public RedisPasswordResetCodeStore(IConnectionMultiplexer redis)
        {
            _redis = redis.GetDatabase();
        }

        public async Task<string> GenerateCodeAsync(Guid userId, TimeSpan ttl)
        {
            var code = Random.Shared.Next(100000, 999999).ToString();
            await _redis.StringSetAsync(GetKey(userId), code, ttl);
            return code;
        }

        public async Task<bool> TryValidateAndConsumeAsync(Guid userId, string code)
        {
            var key = GetKey(userId);
            var stored = await _redis.StringGetAsync(key);

            if (stored.IsNullOrEmpty || !string.Equals(stored, code, StringComparison.Ordinal))
            {
                return false;
            }

            // Tüketildi — bir daha kullanılamasın diye hemen sil (TTL'in dolmasını beklemeden).
            await _redis.KeyDeleteAsync(key);
            return true;
        }

        private static string GetKey(Guid userId) => $"{KeyPrefix}{userId}";
    }
}