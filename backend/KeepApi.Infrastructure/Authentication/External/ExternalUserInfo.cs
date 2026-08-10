namespace KeepApi.Infrastructure.Authentication.External
{
    /// <summary>Sağlayıcıdan (Google/Microsoft/GitHub) dönen, normalize edilmiş kullanıcı bilgisi.</summary>
    public sealed record ExternalUserInfo(
        string ProviderKey,
        string Email,
        bool EmailVerified,
        string FirstName,
        string LastName);

    /// <summary>
    /// Bir OAuth authorization code'unu ilgili sağlayıcıda access token'a çevirip, sağlayıcının kullanıcı bilgisi uç noktasından profil çeker.
    /// </summary>
    public interface IExternalOAuthClient
    {
        /// <param name="provider">"google" | "microsoft" | "github"</param>
        /// <param name="code">Frontend'in provider'dan aldığı authorization code.</param>
        /// <param name="redirectUri">Frontend'in authorize isteğinde kullandığı redirect_uri (birebir aynı olmalı).</param>
        Task<ExternalUserInfo> ExchangeAndGetUserInfoAsync(string provider, string code, string redirectUri);
    }

    /// <summary>Sağlayıcı adı bilinmiyorsa veya sağlayıcı tarafı bir hata dönerse fırlatılır.</summary>
    public sealed class ExternalAuthException(string message) : Exception(message);
}