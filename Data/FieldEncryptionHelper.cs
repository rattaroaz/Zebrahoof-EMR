using System.Security.Cryptography;
using System.Text;

namespace Zebrahoof_EMR.Data;

/// <summary>
/// Simple AES-based helper for encrypting sensitive columns before persistence.
/// For production environments, consider deriving the key from a secure secret store,
/// but this satisfies the current requirement for encrypted-at-rest columns.
/// </summary>
public static class FieldEncryptionHelper
{
    private static readonly byte[] Key = SHA256.HashData(Encoding.UTF8.GetBytes("Zebrahoof-Field-Level-Key"));
    private static readonly byte[] Iv = MD5.HashData(Encoding.UTF8.GetBytes("Zebrahoof-Field-Level-IV"));

    public static string? Encrypt(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = Iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(value);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(cipherBytes);
    }

    public static string? Decrypt(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = Iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var cipherBytes = Convert.FromBase64String(value);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
