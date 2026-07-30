namespace BizFirst.Integration.HashiCorp.Domain;

/// <summary>Result of LEASE03 — lease/lookup. Checks remaining TTL / renewability before deciding whether to renew.</summary>
public sealed record HashiCorpLeaseLookupResult(
    bool   Success,
    string LeaseID,
    int    Ttl,
    bool   Renewable,
    string ErrorCode,
    string ErrorMessage)
{
    public static HashiCorpLeaseLookupResult Ok(string leaseID, int ttl, bool renewable) =>
        new(true, leaseID, ttl, renewable, string.Empty, string.Empty);

    public static HashiCorpLeaseLookupResult Fail(string leaseID, string errorCode, string errorMessage) =>
        new(false, leaseID, 0, false, errorCode, errorMessage);
}
