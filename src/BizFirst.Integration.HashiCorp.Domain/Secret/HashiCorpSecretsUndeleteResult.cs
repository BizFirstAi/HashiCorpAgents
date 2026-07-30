namespace BizFirst.Integration.HashiCorp.Domain;

/// <summary>Result of SEC04 — secret/undelete (KV v2 only). Restores soft-deleted versions.</summary>
public sealed record HashiCorpSecretsUndeleteResult(
    bool   Success,
    string Mount,
    string Path,
    IReadOnlyList<int> Versions,
    string ErrorCode,
    string ErrorMessage)
{
    public static HashiCorpSecretsUndeleteResult Ok(string mount, string path, IReadOnlyList<int> versions) =>
        new(true, mount, path, versions, string.Empty, string.Empty);

    public static HashiCorpSecretsUndeleteResult Fail(string mount, string path, string errorCode, string errorMessage) =>
        new(false, mount, path, [], errorCode, errorMessage);
}
