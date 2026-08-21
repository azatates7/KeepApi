using System.Security.Cryptography;
using System.Text;
using KeepApi.Data.Context;
using KeepApi.Data.Entity;
using KeepApi.Infrastructure.Authentication.Jwt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KeepApi.Infrastructure.Authentication.RefreshTokens
{
    public sealed class RefreshTokenService : IRefreshTokenService
    {
        private readonly KeepDbContext _context;
        private readonly JwtSettings _settings;

        public RefreshTokenService(KeepDbContext context, IOptions<JwtSettings> options)
        {
            _context = context;
            _settings = options.Value;
        }

        public async Task<(string Token, DateTime ExpiresAt)> IssueAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var rawToken = GenerateRawToken();
            var expiresAt = DateTime.UtcNow.AddDays(_settings.RefreshTokenExpireDays);

            _context.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = Hash(rawToken),
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync(cancellationToken);

            return (rawToken, expiresAt);
        }

        public async Task<(string Token, DateTime ExpiresAt, Guid UserId)?> ValidateAndRotateAsync(string rawToken, CancellationToken cancellationToken = default)
        {
            var hash = Hash(rawToken);

            var existing = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

            if (existing is null || existing.RevokedAt != null || existing.ExpiresAt < DateTime.UtcNow)
            {
                return null;
            }

            // Rotation: eski token'ı iptal edip yenisini üretiyoruz. Aynı token iki kez
            // /refresh'e gönderilmeye çalışılırsa (ör. çalınmışsa ve hem saldırgan hem gerçek
            // kullanıcı kullanmayı denerse) ikinci deneme RevokedAt dolu olduğu için reddedilir.
            var (newRawToken, newExpiresAt) = await IssueAsync(existing.UserId, cancellationToken);

            existing.RevokedAt = DateTime.UtcNow;
            existing.ReplacedByTokenHash = Hash(newRawToken);
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync(cancellationToken);

            return (newRawToken, newExpiresAt, existing.UserId);
        }

        public async Task RevokeAsync(string rawToken, CancellationToken cancellationToken = default)
        {
            var hash = Hash(rawToken);

            var existing = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

            if (existing is null || existing.RevokedAt != null)
            {
                return;
            }

            existing.RevokedAt = DateTime.UtcNow;
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync(cancellationToken);
        }

        private static string GenerateRawToken()
        {
            // 512 bit rastgelelik, URL-safe base64.
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes);
        }

        private static string Hash(string rawToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToHexString(bytes);
        }
    }
}