namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

/// <summary>SEC07 — secrets/readMetadata (KV v2 only). Version history, cas_required, max_versions, delete_version_after.</summary>
internal sealed class SecretsReadMetadataInfo : BaseHashiCorpOperationInfo
{
    public string? Path { get; private set; }

    protected override bool RequiresMount => true;

    public override void LoadFrom(ConfigDataPropertyBag reader)
    {
        base.LoadFrom(reader);
        Path = reader.ReadConfigByKeyDefaultNull("path");
    }

    protected override (string Code, string Message)? ValidateOperation() =>
        string.IsNullOrWhiteSpace(Path) ? ("HASHICORP_INVALID_CONFIGURATION", "Config key 'path' is required for secrets/readMetadata.") : null;

    protected override void AddOperationFields(Dictionary<string, object> d)
    {
        d["path"] = Path ?? string.Empty;
    }
}
