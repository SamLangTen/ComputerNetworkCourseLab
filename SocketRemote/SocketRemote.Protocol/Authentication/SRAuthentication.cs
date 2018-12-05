using System;
using System.Collections.Generic;
using System.Text;
using System.Security;
using System.Security.Cryptography;
using System.IO;

namespace SocketRemote.Protocol.Authentication
{
    public class SRAuthentication
    {
        private RijndaelManaged _rijndaelCipher;
        public SRAuthentication(byte[] SecretKey, byte[] IV)
        {
            _rijndaelCipher = new RijndaelManaged();
            _rijndaelCipher.BlockSize = 128;
            _rijndaelCipher.KeySize = 128;
            _rijndaelCipher.Key = SecretKey;
            _rijndaelCipher.IV = IV;
            _rijndaelCipher.Padding = PaddingMode.PKCS7;
            _rijndaelCipher.Mode = CipherMode.CBC;
        }

        public byte[] Decrpyt(byte[] chiperText)
        {
            var transformer = _rijndaelCipher.CreateDecryptor();
            return transformer.TransformFinalBlock(chiperText, 0, chiperText.Length);
        }

        public byte[] Encrpyt(byte[] plainText)
        {
            var transformer = _rijndaelCipher.CreateEncryptor();
            return transformer.TransformFinalBlock(plainText, 0, plainText.Length);
        }
    }
}
