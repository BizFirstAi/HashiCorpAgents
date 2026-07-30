namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

/// <summary>TOK05 — token/revokeByAccessor. Cleans up a short-lived child token after a sub-task completes.</summary>
internal sealed class TokenRevokeByAccessorInfo : BaseHashiCorpOperationInfo
{
    public string? Accessor { get; private set; }

    public override void LoadFrom(ConfigDataPropertyBag reader)
    {
        base.LoadFrom(reader);
        Accessor = reader.ReadConfigByKeyDefaultNull("accessor");
    }

    protected override (string Code, string Message)? ValidateOperation() =>
        string.IsNullOrWhiteSpace(Accessor) ? ("HASHICORP_INVALID_CONFIGURATION", "Config key 'accessor' is required for token/revokeByAccessor.") : null;

    protected override void AddOperationFields(Dictionary<string, object> d)
    {
        d["accessor"] = Accessor ?? string.Empty;
    }
}
