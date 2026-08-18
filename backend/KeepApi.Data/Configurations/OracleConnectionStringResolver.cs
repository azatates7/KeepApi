using KeepApi.Data.Configurations;
using Microsoft.Extensions.Configuration;

namespace KeepApi.Data.Configurations
{
    /// <summary>
    /// Oracle bağlantı dizesini çözer. Kaynak kodda/appsettings.json'da gerçek şifre
    /// hiçbir zaman tutulmaz. Öncelik sırası:
    ///   1) ConnectionStrings:OracleConnection (ConnectionStrings__OracleConnection ortam
    ///      değişkeni ile beslenir — Docker/Linux/production için tam connection string)
    ///   2) Windows Credential Manager (sadece Windows'ta doğrudan çalıştırırken;
    ///      ConnectionStrings:OracleConnectionTemplate şablonundaki {0} placeholder'ı
    ///      Credential Manager'dan okunan şifreyle doldurulur)
    /// İkisi de sonuç vermezse InvalidOperationException fırlatılır — uygulama
    /// hiçbir bağlantı bilgisi olmadan sessizce ayağa kalkmaz.
    /// </summary>
    public static class OracleConnectionStringResolver
    {
        private const string WindowsCredentialTargetName = "KeepApi:OracleConnection";

        public static string Resolve(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("OracleConnection");

            if (string.IsNullOrWhiteSpace(connectionString) && OperatingSystem.IsWindows())
            {
                var template = configuration.GetConnectionString("OracleConnectionTemplate");
                var password = WindowsCredentialManager.ReadPassword(WindowsCredentialTargetName);

                if (!string.IsNullOrWhiteSpace(template) && !string.IsNullOrWhiteSpace(password))
                {
                    connectionString = string.Format(template, password);
                }
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "OracleConnection tanımlı değil. ConnectionStrings__OracleConnection ortam değişkenini ayarlayın " +
                    "ya da Windows Credential Manager'a 'cmdkey /generic:KeepApi:OracleConnection /user:keepapi_app /pass:...' ile bir kayıt ekleyin.");
            }

            return connectionString;
        }
    }
}