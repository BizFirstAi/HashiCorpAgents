namespace BizFirst.Integration.HashiCorp.Domain;

/// <summary>Result of SEC05 — secret/destroy (KV v2 only). Permanent — no undo, unlike SEC03.</summary>
public sealed record HashiCorpSecretsDestroyResult(
    bool   Success,
    string Mount,
    string Path,
    IReadOnlyList<int> Versions,
    string ErrorCode,
    string ErrorMessage)
{
    public static HashiCorpSecretsDestroyResult Ok(string mount, string path, IReadOnlyList<int> versions) =>
        new(true, mount, path, versions, string.Empty, string.Empty);

    public static HashiCorpSecretsDestroyResult Fail(string mount, string path, string errorCode, string errorMessage) =>
        new(false, mount, path, [], errorCode, errorMessage);
}
