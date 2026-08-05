using System.Collections.Concurrent;

namespace KeepApi.Infrastructure.Authentication.PasswordReset
{
    public interface IPasswordResetCodeStore
    {
        /// <summary>Kullanıcı için yeni bir doğrulama kodu üretir ve saklar (eskisinin yerine geçer).</summary>
        string GenerateCode(Guid userId, TimeSpan ttl);

        /// <summary>Kodu doğrular; doğruysa tüketir (bir daha kullanılamaz) ve true döner.</summary>
        bool TryValidateAndConsume(Guid userId, string code);
    }

    public sealed class InMemoryPasswordResetCodeStore : IPasswordResetCodeStore
    {
        private sealed record Entry(string Code, DateTime ExpiresAtUtc);

        private readonly ConcurrentDictionary<Guid, Entry> _codes = new();

        public string GenerateCode(Guid userId, TimeSpan ttl)
        {
            var code = Random.Shared.Next(100000, 999999).ToString();
            _codes[userId] = new Entry(code, DateTime.UtcNow.Add(ttl));
            return code;
        }

        public bool TryValidateAndConsume(Guid userId, string code)
        {
            if (!_codes.TryGetValue(userId, out var entry))
            {
                return false;
            }

            if (entry.ExpiresAtUtc < DateTime.UtcNow)
            {
                _codes.TryRemove(userId, out _);
                return false;
            }

            if (!string.Equals(entry.Code, code, StringComparison.Ordinal))
            {
                return false;
            }

            _codes.TryRemove(userId, out _);
            return true;
        }
    }
}