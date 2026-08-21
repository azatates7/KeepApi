namespace KeepApi.Infrastructure.Authentication.RefreshTokens
{
    public interface IRefreshTokenService
    {
        /// <summary>Yeni bir refresh token üretir, hash'ini DB'ye yazar, ham (opak) token değerini döner.
        /// Ham değer sadece bu çağrının dönüşünde vardır — bir daha DB'den okunamaz.</summary>
        Task<(string Token, DateTime ExpiresAt)> IssueAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>Ham token'ı doğrular; geçerliyse (süresi dolmamış, iptal edilmemiş) eskisini iptal edip
        /// yerine yenisini üretir (rotation) ve döner. Geçersizse null döner — çağıran taraf bunu
        /// 401 olarak ele almalı.</summary>
        Task<(string Token, DateTime ExpiresAt, Guid UserId)?> ValidateAndRotateAsync(string rawToken, CancellationToken cancellationToken = default);

        /// <summary>Verilen token'ı (ör. logout'ta) iptal eder. Token bulunamazsa sessizce döner.</summary>
        Task RevokeAsync(string rawToken, CancellationToken cancellationToken = default);
    }
}