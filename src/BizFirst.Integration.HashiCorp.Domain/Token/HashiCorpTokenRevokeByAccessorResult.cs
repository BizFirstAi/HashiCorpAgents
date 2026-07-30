namespace BizFirst.Integration.HashiCorp.Domain;

/// <summary>Result of TOK05 — token/revokeByAccessor. Cleans up a short-lived child token without needing its raw value.</summary>
public sealed record HashiCorpTokenRevokeByAccessorResult(
    bool   Success,
    string Accessor,
    string ErrorCode,
    string ErrorMessage)
{
    public static HashiCorpTokenRevokeByAccessorResult Ok(string accessor) =>
        new(true, accessor, string.Empty, string.Empty);

    public static HashiCorpTokenRevokeByAccessorResult Fail(string accessor, string errorCode, string errorMessage) =>
        new(false, accessor, errorCode, errorMessage);
}
