namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

/// <summary>SEC01 — secrets/read. Reads a secret's data at an optional specific version (v2) or the current value (v1, no versioning).</summary>
internal sealed class SecretsReadInfo : BaseHashiCorpOperationInfo
{
    public string? Path { get; private set; }
    public int? Version { get; private set; }

    protected override bool RequiresMount => true;

    public override void LoadFrom(ConfigDataPropertyBag reader)
    {
        base.LoadFrom(reader);
        Path = reader.ReadConfigByKeyDefaultNull("path");
        Version = reader.ReadConfigByKey_Int("version");
    }

    protected override (string Code, string Message)? ValidateOperation() =>
        string.IsNullOrWhiteSpace(Path) ? ("HASHICORP_INVALID_CONFIGURATION", "Config key 'path' is required for secrets/read.") : null;

    protected override void AddOperationFields(Dictionary<string, object> d)
    {
        d["path"] = Path ?? string.Empty;
        if (Version.HasValue) d["version"] = Version.Value;
    }
}
