using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace KeepApi.Data.Configurations
{
    /// <summary>
    /// Windows Credential Manager'dan (Kimlik Bilgisi Yöneticisi) "Generic" türde
    /// kaydedilmiş bir kimlik bilgisini okur. Sadece Windows'ta çalışır (Docker/Linux
    /// container'da kullanılamaz — orada ortam değişkeni/User Secrets kullanılmalı).
    ///
    /// Kayıt eklemek için (bir kerelik, uygulamayı hangi Windows kullanıcısı
    /// çalıştıracaksa o hesap altında):
    ///   cmdkey /generic:KeepApi:OracleConnection /user:keepapi_app /pass:GERÇEK_ŞİFRE
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class WindowsCredentialManager
    {
        /// <summary>
        /// Verilen "target" adıyla kaydedilmiş genel (generic) kimlik bilgisinin
        /// şifresini döner. Kayıt yoksa veya okunamazsa null döner.
        /// </summary>
        public static string? ReadPassword(string targetName)
        {
            if (!OperatingSystem.IsWindows())
            {
                return null;
            }

            if (!CredRead(targetName, CRED_TYPE_GENERIC, 0, out var credentialPtr))
            {
                return null; // Kayıt bulunamadı ya da farklı bir Windows kullanıcısı altında saklanmış
            }

            try
            {
                var credential = Marshal.PtrToStructure<CREDENTIAL>(credentialPtr);

                if (credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0)
                {
                    return null;
                }

                var passwordBytes = new byte[credential.CredentialBlobSize];
                Marshal.Copy(credential.CredentialBlob, passwordBytes, 0, (int)credential.CredentialBlobSize);

                // CredWrite/cmdkey şifreyi UTF-16LE (Unicode) olarak saklar.
                return System.Text.Encoding.Unicode.GetString(passwordBytes);
            }
            finally
            {
                CredFree(credentialPtr);
            }
        }

        private const int CRED_TYPE_GENERIC = 1;

        [StructLayout(LayoutKind.Sequential)]
        private struct CREDENTIAL
        {
            public uint Flags;
            public uint Type;
            public IntPtr TargetName;
            public IntPtr Comment;
            public long LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public uint Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            public IntPtr TargetAlias;
            public IntPtr UserName;
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credentialPtr);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern void CredFree(IntPtr credentialPtr);
    }
}