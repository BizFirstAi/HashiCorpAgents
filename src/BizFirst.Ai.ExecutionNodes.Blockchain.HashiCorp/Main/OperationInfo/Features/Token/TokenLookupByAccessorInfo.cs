namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

/// <summary>TOK02 — token/lookupByAccessor. Inspects a different token (e.g. a child token minted earlier) without needing its raw value.</summary>
internal sealed class TokenLookupByAccessorInfo : BaseHashiCorpOperationInfo
{
    public string? Accessor { get; private set; }

    public override void LoadFrom(ConfigDataPropertyBag reader)
    {
        base.LoadFrom(reader);
        Accessor = reader.ReadConfigByKeyDefaultNull("accessor");
    }

    protected override (string Code, string Message)? ValidateOperation() =>
        string.IsNullOrWhiteSpace(Accessor) ? ("HASHICORP_INVALID_CONFIGURATION", "Config key 'accessor' is required for token/lookupByAccessor.") : null;

    protected override void AddOperationFields(Dictionary<string, object> d)
    {
        d["accessor"] = Accessor ?? string.Empty;
    }
}
