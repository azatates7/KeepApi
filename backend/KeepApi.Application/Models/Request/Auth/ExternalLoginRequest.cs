namespace KeepApi.Application.Models.Request.Auth
{
    /// <summary>
    /// Frontend, OAuth sağlayıcısından (Google/Microsoft/GitHub) dönen
    /// authorization "code"unu ve o istekte kullandığı redirect_uri'yi buraya gönderir.
    /// Sağlayıcı adı route'tan gelir (api/auth/external/{provider}).
    /// </summary>
    public class ExternalLoginRequest
    {
        public string Code { get; set; } = string.Empty;

        public string RedirectUri { get; set; } = string.Empty;
    }
}