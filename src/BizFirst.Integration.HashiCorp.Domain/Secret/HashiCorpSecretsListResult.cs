namespace BizFirst.Integration.HashiCorp.Domain;

/// <summary>
/// Result of SEC06 — secret/list. An empty <paramref name="keys"/> list (via <c>Ok</c>) covers both
/// "folder has no children" and Vault's 404-on-empty-path case, which the HTTP client already
/// normalizes upstream — this is never mapped to an error.
/// </summary>
public sealed record HashiCorpSecretsListResult(
    bool   Success,
    string Mount,
    string Path,
    IReadOnlyList<string> Keys,
    string ErrorCode,
    string ErrorMessage)
{
    public static HashiCorpSecretsListResult Ok(string mount, string path, IReadOnlyList<string> keys) =>
        new(true, mount, path, keys, string.Empty, string.Empty);

    public static HashiCorpSecretsListResult Fail(string mount, string path, string errorCode, string errorMessage) =>
        new(false, mount, path, [], errorCode, errorMessage);
}
