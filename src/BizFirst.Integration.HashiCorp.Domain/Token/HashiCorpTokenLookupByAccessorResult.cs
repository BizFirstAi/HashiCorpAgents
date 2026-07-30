namespace BizFirst.Integration.HashiCorp.Domain;

/// <summary>Result of TOK02 — token/lookupByAccessor. Inspects a different token without needing its raw value.</summary>
public sealed record HashiCorpTokenLookupByAccessorResult(
    bool                Success,
    HashiCorpTokenInfo? Token,
    string              ErrorCode,
    string              ErrorMessage)
{
    public static HashiCorpTokenLookupByAccessorResult Ok(HashiCorpTokenInfo token) =>
        new(true, token, string.Empty, string.Empty);

    public static HashiCorpTokenLookupByAccessorResult Fail(string errorCode, string errorMessage) =>
        new(false, null, errorCode, errorMessage);
}
