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
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bytes
        RandomNumberGenerator.Fill(nonce);
        var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes
        var cipherText = new byte[plainBytes.Length];
        using var aesGcm = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
        aesGcm.Encrypt(nonce, plainBytes, cipherText, tag);
        // Format: nonce(12) | tag(16) | ciphertext
        var combined = new byte[nonce.Length + tag.Length + cipherText.Length];
        nonce.CopyTo(combined, 0);
        tag.CopyTo(combined, nonce.Length);
        cipherText.CopyTo(combined, nonce.Length + tag.Length);
        return Convert.ToBase64String(combined);
    }

    public string Decrypt(string cipherTextBase64)
    {
        var combined = Convert.FromBase64String(cipherTextBase64);
        var nonce = combined[..12];
        var tag = combined[12..28];
        var cipherText = combined[28..];
        var plainBytes = new byte[cipherText.Length];
        using var aesGcm = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
        aesGcm.Decrypt(nonce, cipherText, tag, plainBytes);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
