using System.Security.Cryptography;
using System.Text;
using AiCostMonitor.Core.Interfaces;

namespace AiCostMonitor.Api.Services;

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public EncryptionService(IConfiguration config)
    {
        var keyBase64 = config["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption:Key is not configured.");
        _key = Convert.FromBase64String(keyBase64);
        if (_key.Length != 32)
            throw new InvalidOperationException("Encryption:Key must be 32 bytes (256-bit) base64-encoded.");
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Format: base64(IV + ciphertext)
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        aes.IV.CopyTo(result, 0);
        cipherBytes.CopyTo(result, aes.IV.Length);
        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        var data = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _key;

        var iv = data[..16];
        var cipher = data[16..];
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }
}