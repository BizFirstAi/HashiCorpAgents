namespace BizFirst.Integration.HashiCorp.Domain;

/// <summary>Result of LEASE02 — lease/revoke. Immediately invalidates a lease.</summary>
public sealed record HashiCorpLeaseRevokeResult(
    bool   Success,
    string LeaseID,
    string ErrorCode,
    string ErrorMessage)
{
    public static HashiCorpLeaseRevokeResult Ok(string leaseID) =>
        new(true, leaseID, string.Empty, string.Empty);

    public static HashiCorpLeaseRevokeResult Fail(string leaseID, string errorCode, string errorMessage) =>
        new(false, leaseID, errorCode, errorMessage);
}
