using KeepApi.Common.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeepApi.Data.Entity
{
    [Table("RefreshTokens")]
    public class RefreshToken : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }

        public ApplicationUser User { get; set; } = null!;

        /// <summary>Ham refresh token hiçbir yerde saklanmaz — sadece SHA-256 hash'i. Doğrulama,
        /// gelen değerin hash'i bu alanla karşılaştırılarak yapılır (bkz. RefreshTokenService).</summary>
        public string TokenHash { get; set; } = null!;

        public DateTime ExpiresAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        /// <summary>Rotasyonda bu token'ın yerine geçen yeni token'ın hash'i (audit/zincir takibi için).</summary>
        public string? ReplacedByTokenHash { get; set; }
    }
}