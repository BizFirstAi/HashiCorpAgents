namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

/// <summary>SEC05 — secrets/destroy (KV v2 only). Permanently destroys the specified versions — no undo, unlike SEC03.</summary>
internal sealed class SecretsDestroyInfo : BaseHashiCorpOperationInfo
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

    protected override (string Code, string Message)? ValidateOperation()
    {
        if (string.IsNullOrWhiteSpace(Path))
            return ("HASHICORP_INVALID_CONFIGURATION", "Config key 'path' is required for secrets/destroy.");
        if (Versions.Count == 0)
            return ("HASHICORP_INVALID_CONFIGURATION", "Config key 'versions' must contain at least one version for secrets/destroy.");
        return null;
    }

    protected override void AddOperationFields(Dictionary<string, object> d)
    {
        d["path"] = Path ?? string.Empty;
        d["versions"] = Versions;
    }
}
