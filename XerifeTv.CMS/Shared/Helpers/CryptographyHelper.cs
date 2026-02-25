using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace XerifeTv.CMS.Shared.Helpers;

public static class CryptographyHelper
{
    public static string Encrypt(string text, string key)
    {
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("Text cannot be null or empty.");

        byte[] compressedBytes = Compress(text);

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        aes.Key = DeriveKey(key);
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();

        byte[] encryptedBytes;
        using (var ms = new MemoryStream())
        {
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cryptoStream = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cryptoStream.Write(compressedBytes, 0, compressedBytes.Length);
                cryptoStream.FlushFinalBlock();
            }

            encryptedBytes = ms.ToArray();
        }

        return Convert.ToBase64String(encryptedBytes);
    }

    public static string Decrypt(string text, string key)
    {
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("Text cannot be null or empty.");

        byte[] fullCipher = Convert.FromBase64String(text);

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        aes.Key = DeriveKey(key);

        byte[] iv = new byte[16];
        Array.Copy(fullCipher, 0, iv, 0, iv.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();

        byte[] decryptedBytes;
        using (var ms = new MemoryStream())
        {
            using (var cryptoStream = new CryptoStream(
                new MemoryStream(fullCipher, 16, fullCipher.Length - 16),
                decryptor,
                CryptoStreamMode.Read))
            {
                cryptoStream.CopyTo(ms);
            }

            decryptedBytes = ms.ToArray();
        }

        return Decompress(decryptedBytes);
    }

    private static byte[] DeriveKey(string key)
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(Encoding.UTF8.GetBytes(key));
    }

    private static byte[] Compress(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);

        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
        {
            gzip.Write(bytes, 0, bytes.Length);
        }

        return output.ToArray();
    }

    private static string Decompress(byte[] bytes)
    {
        using var input = new MemoryStream(bytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();

        gzip.CopyTo(output);
        return Encoding.UTF8.GetString(output.ToArray());
    }
}
