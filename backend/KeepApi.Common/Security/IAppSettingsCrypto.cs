using System;
using System.Collections.Generic;
using System.Text;

namespace KeepApi.Common.Security
{
    public interface IAppSettingsCrypto
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
    }
}
