namespace BizFirst.Integration.HashiCorp.Services.Http;

/// <summary>
/// Timeout/retry configuration for <see cref="HashiCorpApiClient"/>. Deliberately carries no base
/// URL — Vault is self-hosted per tenant, so every call takes the tenant's <c>vaultAddress</c>
/// per-call via <see cref="Domain.HashiCorpCallContext"/> rather than a constant baked in here.
/// </summary>
public sealed class HashiCorpApiClientOptions
{
    /// <summary>Max retry attempts for a retryable failure (429 from a fronting proxy, or 5xx other than sealed/standby codes).</summary>
    public int MaxRetries { get; init; } = 2;

    /// <summary>Initial backoff delay before the first retry; doubles on each subsequent attempt.</summary>
    public TimeSpan InitialRetryDelay { get; init; } = TimeSpan.FromSeconds(1);
}
