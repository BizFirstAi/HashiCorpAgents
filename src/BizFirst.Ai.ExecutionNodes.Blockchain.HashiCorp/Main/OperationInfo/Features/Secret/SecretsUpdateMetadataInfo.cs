namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

/// <summary>SEC08 — secrets/updateMetadata (KV v2 only). Configures retention (max_versions, cas_required) without writing new data.</summary>
internal sealed class SecretsUpdateMetadataInfo : BaseHashiCorpOperationInfo
{
    public string? Path { get; private set; }
    public int? MaxVersions { get; private set; }
    public bool? CasRequired { get; private set; }

    protected override bool RequiresMount => true;

    public override void LoadFrom(ConfigDataPropertyBag reader)
    {
        base.LoadFrom(reader);
        Path = reader.ReadConfigByKeyDefaultNull("path");
        MaxVersions = reader.ReadConfigByKey_Int("maxVersions");
        var casRequiredRaw = reader.ReadConfigByKeyDefaultNull("casRequired");
        CasRequired = casRequiredRaw is null ? null : bool.TryParse(casRequiredRaw, out var casRequired) && casRequired;
    }

    protected override (string Code, string Message)? ValidateOperation() =>
        string.IsNullOrWhiteSpace(Path) ? ("HASHICORP_INVALID_CONFIGURATION", "Config key 'path' is required for secrets/updateMetadata.") : null;

    protected override void AddOperationFields(Dictionary<string, object> d)
    {
        d["path"] = Path ?? string.Empty;
        if (MaxVersions.HasValue) d["maxVersions"] = MaxVersions.Value;
        if (CasRequired.HasValue) d["casRequired"] = CasRequired.Value;
    }
}
