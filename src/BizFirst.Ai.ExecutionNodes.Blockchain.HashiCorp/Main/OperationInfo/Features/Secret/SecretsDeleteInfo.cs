namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

/// <summary>SEC03 — secrets/delete. Soft delete of the given versions on v2 (permanent on v1). Omitted/empty <see cref="Versions"/> deletes the latest version.</summary>
internal sealed class SecretsDeleteInfo : BaseHashiCorpOperationInfo
{
    public string? Path { get; private set; }
    public IReadOnlyList<int> Versions { get; private set; } = [];

    protected override bool RequiresMount => true;

    public override void LoadFrom(ConfigDataPropertyBag reader)
    {
        base.LoadFrom(reader);
        Path = reader.ReadConfigByKeyDefaultNull("path");
        Versions = ReadIntArray(reader, "versions");
    }

    protected override (string Code, string Message)? ValidateOperation() =>
        string.IsNullOrWhiteSpace(Path) ? ("HASHICORP_INVALID_CONFIGURATION", "Config key 'path' is required for secrets/delete.") : null;

    protected override void AddOperationFields(Dictionary<string, object> d)
    {
        d["path"] = Path ?? string.Empty;
        d["versions"] = Versions;
    }
}
