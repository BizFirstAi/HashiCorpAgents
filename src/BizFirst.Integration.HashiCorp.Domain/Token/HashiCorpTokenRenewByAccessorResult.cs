namespace BizFirst.Integration.HashiCorp.Domain;

/// <summary>Result of TOK04 — token/renewByAccessor. Extends a delegated/child token's TTL.</summary>
public sealed record HashiCorpTokenRenewByAccessorResult(
    bool   Success,
    string Accessor,
    int    Ttl,
    string ErrorCode,
    string ErrorMessage)
{
    public static HashiCorpTokenRenewByAccessorResult Ok(string accessor, int ttl) =>
        new(true, accessor, ttl, string.Empty, string.Empty);

    public static HashiCorpTokenRenewByAccessorResult Fail(string errorCode, string errorMessage) =>
        new(false, string.Empty, 0, errorCode, errorMessage);
}
