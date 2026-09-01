namespace JB2026.Api.Models;

public sealed class TwoFactorVerifyRequest
{
    public required string TwoFactorToken { get; init; }

    public required string Code { get; init; }
}

public sealed class TwoFactorSetupResponse
{
    public required string Secret { get; init; }

    public required string ProvisioningUri { get; init; }
}

public sealed class TwoFactorConfirmRequest
{
    public required string Code { get; init; }
}

public sealed class TwoFactorConfirmResponse
{
    public required List<string> RecoveryCodes { get; init; }
}

public sealed class TwoFactorDisableRequest
{
    public required string Password { get; init; }

    public required string Code { get; init; }
}

public sealed class TwoFactorAdminDisableRequest
{
    public required Guid UserId { get; init; }
}

public sealed class TwoFactorStatusResponse
{
    public required bool Enabled { get; init; }
}
