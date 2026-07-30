namespace BizFirst.Integration.HashiCorp.Domain;

/// <summary>Result of SEC07 — secret/read-metadata (KV v2 only). Version history, cas_required, max_versions, delete_version_after.</summary>
public sealed record HashiCorpSecretsReadMetadataResult(
    bool   Success,
    string Mount,
    string Path,
    HashiCorpSecretMetadata? Metadata,
    string ErrorCode,
    string ErrorMessage)
{
    public static HashiCorpSecretsReadMetadataResult Ok(string mount, string path, HashiCorpSecretMetadata metadata) =>
        new(true, mount, path, metadata, string.Empty, string.Empty);

    public static HashiCorpSecretsReadMetadataResult Fail(string mount, string path, string errorCode, string errorMessage) =>
        new(false, mount, path, null, errorCode, errorMessage);
}
