namespace BizFirst.Integration.HashiCorp.Domain;

/// <summary>Result of LEASE01 — lease/renew. Extends a lease on a dynamic secret issued earlier in the workflow.</summary>
public sealed record HashiCorpLeaseRenewResult(
    bool   Success,
    string LeaseID,
    int    Ttl,
    bool   Renewable,
    string ErrorCode,
    string ErrorMessage)
{
    public static HashiCorpLeaseRenewResult Ok(string leaseID, int ttl, bool renewable) =>
        new(true, leaseID, ttl, renewable, string.Empty, string.Empty);

    public static HashiCorpLeaseRenewResult Fail(string leaseID, string errorCode, string errorMessage) =>
        new(false, leaseID, 0, false, errorCode, errorMessage);
}
