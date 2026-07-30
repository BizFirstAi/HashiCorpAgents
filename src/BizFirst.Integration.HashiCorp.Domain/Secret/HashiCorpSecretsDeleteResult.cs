namespace BizFirst.Integration.HashiCorp.Domain;

/// <summary>Result of SEC03 — secret/delete. Soft delete of the given versions on v2 (permanent on v1).</summary>
public sealed record HashiCorpSecretsDeleteResult(
    bool   Success,
    string Mount,
    string Path,
    IReadOnlyList<int> Versions,
    string ErrorCode,
    string ErrorMessage)
{
    public static HashiCorpSecretsDeleteResult Ok(string mount, string path, IReadOnlyList<int> versions) =>
        new(true, mount, path, versions, string.Empty, string.Empty);

    public static HashiCorpSecretsDeleteResult Fail(string mount, string path, string errorCode, string errorMessage) =>
        new(false, mount, path, [], errorCode, errorMessage);
}
