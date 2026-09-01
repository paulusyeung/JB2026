using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using OtpNet;

namespace JB2026.Api.Services;

public sealed class TwoFactorService : ITwoFactorService
{
    private readonly byte[] _encryptionKey;

    public TwoFactorService(IConfiguration configuration)
    {
        var keyBase64 = configuration["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption:Key configuration is missing.");
        _encryptionKey = Convert.FromBase64String(keyBase64);
    }

    public string GenerateSecret()
    {
        var secret = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(secret);
    }

    public string GetProvisioningUri(string userId, string secret, string issuer = "JB2026")
    {
        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(userId)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30";
    }

    public bool ValidateCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
            return false;

        var secretBytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(secretBytes);
        return totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));
    }

    public List<string> GenerateRecoveryCodes(int count = 10)
    {
        var codes = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            var randomBytes = RandomNumberGenerator.GetBytes(6);
            var code = Convert.ToBase64String(randomBytes)
                .Replace("+", "A")
                .Replace("/", "B")
                .Replace("=", "")
                .Substring(0, 10);
            codes.Add(code);
        }
        return codes;
    }

    public string HashRecoveryCodes(List<string> codes)
    {
        var hashedCodes = new List<string>();
        foreach (var code in codes)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = HashWithSalt(code, salt);
            hashedCodes.Add($"{Convert.ToBase64String(hash)}:{Convert.ToBase64String(salt)}");
        }
        return string.Join(",", hashedCodes);
    }

    public (bool Success, string? UpdatedHashedCodes) VerifyRecoveryCode(string hashedCodes, string inputCode)
    {
        if (string.IsNullOrWhiteSpace(hashedCodes) || string.IsNullOrWhiteSpace(inputCode))
            return (false, null);

        var storedHashes = hashedCodes.Split(",", StringSplitOptions.RemoveEmptyEntries);

        for (var i = 0; i < storedHashes.Length; i++)
        {
            var parts = storedHashes[i].Split(':');
            if (parts.Length != 2)
                continue;

            var storedHash = Convert.FromBase64String(parts[0]);
            var salt = Convert.FromBase64String(parts[1]);
            var computedHash = HashWithSalt(inputCode, salt);

            if (CryptographicOperations.FixedTimeEquals(storedHash, computedHash))
            {
                // Remove the used recovery code
                var remainingCodes = storedHashes.Where((_, index) => index != i).ToList();
                return (true, string.Join(",", remainingCodes));
            }
        }

        return (false, null);
    }

    public string EncryptSecret(string secret)
    {
        var plainBytes = Encoding.UTF8.GetBytes(secret);
        var iv = RandomNumberGenerator.GetBytes(16);

        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new byte[iv.Length + encryptedBytes.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, iv.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }

    public string DecryptSecret(string encryptedSecret)
    {
        var encryptedBytes = Convert.FromBase64String(encryptedSecret);
        if (encryptedBytes.Length < 17) // IV (16 bytes) + at least 1 byte of ciphertext
        {
            throw new ArgumentException("Invalid encrypted secret format.", nameof(encryptedSecret));
        }

        var iv = new byte[16];
        var cipher = new byte[encryptedBytes.Length - 16];
        Buffer.BlockCopy(encryptedBytes, 0, iv, 0, 16);
        Buffer.BlockCopy(encryptedBytes, 16, cipher, 0, cipher.Length);

        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }

    private static byte[] HashWithSalt(string value, byte[] salt)
    {
        var valueBytes = Encoding.UTF8.GetBytes(value);
        var combined = new byte[valueBytes.Length + salt.Length];
        Buffer.BlockCopy(valueBytes, 0, combined, 0, valueBytes.Length);
        Buffer.BlockCopy(salt, 0, combined, valueBytes.Length, salt.Length);

        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(combined);
    }
}
