namespace BizFirst.Integration.HashiCorp.Domain;

/// <summary>Result of SEC08 — secret/update-metadata (KV v2 only). Configures retention without writing new data.</summary>
public sealed record HashiCorpSecretsUpdateMetadataResult(
    bool   Success,
    string Mount,
    string Path,
    int?   MaxVersions,
    bool?  CasRequired,
    string ErrorCode,
    string ErrorMessage)
{
    public static HashiCorpSecretsUpdateMetadataResult Ok(string mount, string path, int? maxVersions, bool? casRequired) =>
        new(true, mount, path, maxVersions, casRequired, string.Empty, string.Empty);

    public static HashiCorpSecretsUpdateMetadataResult Fail(string mount, string path, string errorCode, string errorMessage) =>
        new(false, mount, path, null, null, errorCode, errorMessage);
}
