using Microsoft.AspNetCore.DataProtection;

namespace KeepApi.Common.Security
{
    public class AppSettingsCrypto : IAppSettingsCrypto
    {
        private readonly IDataProtector _protector;

        public AppSettingsCrypto(IDataProtectionProvider provider)
            => _protector = provider.CreateProtector("AppSettings.Secrets");

        public string Encrypt(string plainText) => _protector.Protect(plainText);
        public string Decrypt(string cipherText) => _protector.Unprotect(cipherText);
    }
}
