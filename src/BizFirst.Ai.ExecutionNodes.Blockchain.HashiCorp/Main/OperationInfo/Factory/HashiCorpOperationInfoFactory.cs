namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

/// <summary>Maps (resource, operation) config values to the concrete operation DTO, then loads it from the raw config.</summary>
internal static class HashiCorpOperationInfoFactory
{
    internal static BaseHashiCorpOperationInfo? Create(string? resource, string? operation, ConfigDataPropertyBag reader)
    {
        BaseHashiCorpOperationInfo? info = (resource, operation) switch
        {
            // ── secrets (8) ──────────────────────────────────────────────────
            ("secrets", "read")           => new SecretsReadInfo(),
            ("secrets", "write")          => new SecretsWriteInfo(),
            ("secrets", "delete")         => new SecretsDeleteInfo(),
            ("secrets", "undelete")       => new SecretsUndeleteInfo(),
            ("secrets", "destroy")        => new SecretsDestroyInfo(),
            ("secrets", "list")           => new SecretsListInfo(),
            ("secrets", "readMetadata")   => new SecretsReadMetadataInfo(),
            ("secrets", "updateMetadata") => new SecretsUpdateMetadataInfo(),

            // ── token (5) ────────────────────────────────────────────────────
            ("token", "lookupSelf")       => new TokenLookupSelfInfo(),
            ("token", "lookupByAccessor") => new TokenLookupByAccessorInfo(),
            ("token", "renewSelf")        => new TokenRenewSelfInfo(),
            ("token", "renewByAccessor")  => new TokenRenewByAccessorInfo(),
            ("token", "revokeByAccessor") => new TokenRevokeByAccessorInfo(),

            // ── lease (3) ────────────────────────────────────────────────────
            ("lease", "renew")  => new LeaseRenewInfo(),
            ("lease", "revoke") => new LeaseRevokeInfo(),
            ("lease", "lookup") => new LeaseLookupInfo(),

            // ── system (2) — unauthenticated ─────────────────────────────────
            ("system", "health")     => new SystemHealthInfo(),
            ("system", "sealStatus") => new SystemSealStatusInfo(),

            _ => null,
        };

        info?.LoadFrom(reader);
        return info;
    }
}
