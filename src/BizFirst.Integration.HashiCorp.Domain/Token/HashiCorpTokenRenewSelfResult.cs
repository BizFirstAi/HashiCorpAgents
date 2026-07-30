namespace BizFirst.Integration.HashiCorp.Domain;

/// <summary>Result of TOK03 — token/renewSelf. Vault may cap the returned TTL below the requested increment.</summary>
public sealed record HashiCorpTokenRenewSelfResult(
    bool   Success,
    string Accessor,
    int    Ttl,
    string ErrorCode,
    string ErrorMessage)
{
    public static HashiCorpTokenRenewSelfResult Ok(string accessor, int ttl) =>
        new(true, accessor, ttl, string.Empty, string.Empty);

    public static HashiCorpTokenRenewSelfResult Fail(string errorCode, string errorMessage) =>
        new(false, string.Empty, 0, errorCode, errorMessage);
}
