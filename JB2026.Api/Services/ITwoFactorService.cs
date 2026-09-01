namespace JB2026.Api.Services;

public interface ITwoFactorService
{
    string GenerateSecret();

    string GetProvisioningUri(string userId, string secret, string issuer = "JB2026");

    bool ValidateCode(string secret, string code);

    List<string> GenerateRecoveryCodes(int count = 10);

    string HashRecoveryCodes(List<string> codes);

    (bool Success, string? UpdatedHashedCodes) VerifyRecoveryCode(string hashedCodes, string inputCode);

    string EncryptSecret(string secret);

    string DecryptSecret(string encryptedSecret);
}
