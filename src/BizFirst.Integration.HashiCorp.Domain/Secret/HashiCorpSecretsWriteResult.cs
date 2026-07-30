namespace BizFirst.Integration.HashiCorp.Domain;

/// <summary>Result of SEC02 — secret/write. Returns the version number Vault assigned to the write (0 on v1, which has no versioning).</summary>
public sealed record HashiCorpSecretsWriteResult(
    bool   Success,
    string Mount,
    string Path,
    int    Version,
    string ErrorCode,
    string ErrorMessage)
{
    public static HashiCorpSecretsWriteResult Ok(string mount, string path, int version) =>
        new(true, mount, path, version, string.Empty, string.Empty);

    public static HashiCorpSecretsWriteResult Fail(string mount, string path, string errorCode, string errorMessage) =>
        new(false, mount, path, 0, errorCode, errorMessage);
}
