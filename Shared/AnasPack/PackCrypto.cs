using System;
using System.IO;
using System.Security.Cryptography;

namespace AnasPack
{
    // 曲データ（songs.enc）の暗号化/復号。
    // 「zipを開いて解禁前に曲名を見る」といったカジュアルなネタバレの抑止が目的であり、
    // 鍵がアプリに埋め込まれているため本格的な解析には耐えない設計と割り切っている。
    // データ形式: [IV 16バイト][AES-256-CBC + PKCS7 の暗号文]
    public static class PackCrypto
    {
        private const string EmbeddedSecret = "AnasokoHiroba.AnasPack.v1|c4a92f61e8d05b37";

        private static readonly byte[] Salt =
        {
            0x5A, 0x0E, 0x91, 0x3C, 0xB7, 0x44, 0xD2, 0x68,
            0x1F, 0xA3, 0x7D, 0x59, 0xE6, 0x02, 0x8B, 0xC5,
        };

        private const int KeyIterations = 100000;

        public static byte[] Encrypt(byte[] plain, string danHash)
        {
            using (var aes = CreateAes(danHash))
            {
                aes.GenerateIV();
                using (var ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(plain, 0, plain.Length);
                    }
                    return ms.ToArray();
                }
            }
        }

        public static byte[] Decrypt(byte[] data, string danHash)
        {
            if (data == null || data.Length < 17)
            {
                throw new InvalidDataException("暗号化データが壊れています（サイズ不足）。");
            }

            using (var aes = CreateAes(danHash))
            {
                var iv = new byte[16];
                Array.Copy(data, 0, iv, 0, 16);
                aes.IV = iv;

                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(data, 16, data.Length - 16);
                    }
                    return ms.ToArray();
                }
            }
        }

        private static Aes CreateAes(string danHash)
        {
            var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = DeriveKey(danHash);
            return aes;
        }

        // 埋め込みシークレットと対象段位のハッシュから鍵を導出する
        private static byte[] DeriveKey(string danHash)
        {
            using (var kdf = new Rfc2898DeriveBytes(
                EmbeddedSecret + "|" + danHash, Salt, KeyIterations, HashAlgorithmName.SHA256))
            {
                return kdf.GetBytes(32);
            }
        }
    }
}
