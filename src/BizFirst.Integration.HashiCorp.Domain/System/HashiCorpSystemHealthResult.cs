namespace BizFirst.Integration.HashiCorp.Domain;

/// <summary>
/// Result of SYS01 — system/health. Unauthenticated. Vault's <c>/v1/sys/health</c> encodes cluster
/// role in its HTTP status code (200 active, 429 standby, 472 DR secondary, 473 performance standby,
/// 474 standby-unreachable-to-active, 501 uninitialized, 503 sealed, 530 removed from cluster) — this
/// result normalizes all of those into <see cref="Success"/> = true with the state exposed via
/// <see cref="Initialized"/>/<see cref="Sealed"/>/<see cref="Standby"/> and the raw code kept in
/// <see cref="StatusCode"/> for diagnostics. Only a genuine network failure produces <see cref="Fail"/>.
/// </summary>
public sealed record HashiCorpSystemHealthResult(
    bool   Success,
    bool   Initialized,
    bool   Sealed,
    bool   Standby,
    string Version,
    string ClusterName,
    int    StatusCode,
    string ErrorCode,
    string ErrorMessage)
{
    public static HashiCorpSystemHealthResult Ok(
        bool initialized, bool sealedState, bool standby, string version, string clusterName, int statusCode) =>
        new(true, initialized, sealedState, standby, version, clusterName, statusCode, string.Empty, string.Empty);

    public static HashiCorpSystemHealthResult Fail(string errorCode, string errorMessage) =>
        new(false, false, false, false, string.Empty, string.Empty, 0, errorCode, errorMessage);
}
