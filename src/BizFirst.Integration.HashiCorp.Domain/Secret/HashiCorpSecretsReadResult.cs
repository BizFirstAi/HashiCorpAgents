namespace BizFirst.Integration.HashiCorp.Domain;

/// <summary>Result of SEC01 — secret/read. Returns the secret's data map at the resolved version.</summary>
public sealed record HashiCorpSecretsReadResult(
    bool   Success,
    string Mount,
    string Path,
    int    Version,
    IReadOnlyDictionary<string, string> Data,
    string ErrorCode,
    string ErrorMessage)
{
    public static HashiCorpSecretsReadResult Ok(string mount, string path, int version, IReadOnlyDictionary<string, string> data) =>
        new(true, mount, path, version, data, string.Empty, string.Empty);

    public static HashiCorpSecretsReadResult Fail(string mount, string path, string errorCode, string errorMessage) =>
        new(false, mount, path, 0, new Dictionary<string, string>(), errorCode, errorMessage);
}
