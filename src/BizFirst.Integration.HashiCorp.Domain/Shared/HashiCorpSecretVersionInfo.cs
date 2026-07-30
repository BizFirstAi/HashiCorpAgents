namespace BizFirst.Integration.HashiCorp.Domain;

/// <summary>One KV v2 secret version's lifecycle state, as returned by the metadata endpoint.</summary>
public sealed record HashiCorpSecretVersionInfo(
    int             Version,
    DateTimeOffset  CreatedTime,
    DateTimeOffset? DeletionTime,
    bool            Destroyed);
