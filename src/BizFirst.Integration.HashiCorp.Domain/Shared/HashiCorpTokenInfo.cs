namespace BizFirst.Integration.HashiCorp.Domain;

/// <summary>Token lookup result — self or by-accessor. Never carries the raw token value.</summary>
public sealed record HashiCorpTokenInfo(
    string                 Accessor,
    int                    Ttl,
    bool                   Renewable,
    IReadOnlyList<string>  Policies,
    string                 DisplayName);
