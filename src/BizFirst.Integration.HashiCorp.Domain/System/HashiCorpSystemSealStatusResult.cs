namespace BizFirst.Integration.HashiCorp.Domain;

/// <summary>
/// Result of SYS02 — system/sealStatus. Unauthenticated; answers even when Vault is sealed —
/// useful as a workflow pre-flight check before SEC01/TOK01/etc. <c>T</c>/<c>N</c> are kept as
/// Vault itself documents them (Shamir key-share threshold notation), not renamed.
/// </summary>
public sealed record HashiCorpSystemSealStatusResult(
    bool   Success,
    bool   Sealed,
    int    T,
    int    N,
    int    Progress,
    string ErrorCode,
    string ErrorMessage)
{
    public static HashiCorpSystemSealStatusResult Ok(bool sealedState, int t, int n, int progress) =>
        new(true, sealedState, t, n, progress, string.Empty, string.Empty);

    public static HashiCorpSystemSealStatusResult Fail(string errorCode, string errorMessage) =>
        new(false, false, 0, 0, 0, errorCode, errorMessage);
}
