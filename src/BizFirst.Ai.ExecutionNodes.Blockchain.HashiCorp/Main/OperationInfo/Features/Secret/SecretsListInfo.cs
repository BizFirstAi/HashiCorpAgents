namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

/// <summary>SEC06 — secrets/list. Lists key names directly under a path (non-recursive, Vault "folder listing" semantics).</summary>
internal sealed class SecretsListInfo : BaseHashiCorpOperationInfo
{
    public string? Path { get; private set; }

    protected override bool RequiresMount => true;

    public override void LoadFrom(ConfigDataPropertyBag reader)
    {
        base.LoadFrom(reader);
        Path = reader.ReadConfigByKeyDefaultNull("path");
    }

    protected override (string Code, string Message)? ValidateOperation() =>
        string.IsNullOrWhiteSpace(Path) ? ("HASHICORP_INVALID_CONFIGURATION", "Config key 'path' is required for secrets/list.") : null;

    protected override void AddOperationFields(Dictionary<string, object> d)
    {
        d["path"] = Path ?? string.Empty;
    }
}
