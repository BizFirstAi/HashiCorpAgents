namespace BizFirst.Integration.HashiCorp.Domain;

/// <summary>Result of TOK01 — token/lookupSelf. The credential-health-check candidate.</summary>
public sealed record HashiCorpTokenLookupSelfResult(
    bool               Success,
    HashiCorpTokenInfo? Token,
    string             ErrorCode,
    string             ErrorMessage)
{
    public static HashiCorpTokenLookupSelfResult Ok(HashiCorpTokenInfo token) =>
        new(true, token, string.Empty, string.Empty);

    public static HashiCorpTokenLookupSelfResult Fail(string errorCode, string errorMessage) =>
        new(false, null, errorCode, errorMessage);
}
