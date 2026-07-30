namespace BizFirst.Integration.HashiCorp.Domain;

/// <summary>KV v2 secret metadata — version history and retention configuration for a path.</summary>
public sealed record HashiCorpSecretMetadata(
    string                                       Path,
    int                                          CurrentVersion,
    int                                          OldestVersion,
    int                                          MaxVersions,
    bool                                         CasRequired,
    IReadOnlyList<HashiCorpSecretVersionInfo>    Versions);
