namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

/// <summary>TOK04 — token/renewByAccessor. Extends a delegated/child token's TTL.</summary>
internal sealed class TokenRenewByAccessorInfo : BaseHashiCorpOperationInfo
{
    public string? Accessor { get; private set; }
    public int? IncrementSeconds { get; private set; }

    public override void LoadFrom(ConfigDataPropertyBag reader)
    {
        base.LoadFrom(reader);
        Accessor = reader.ReadConfigByKeyDefaultNull("accessor");
        IncrementSeconds = reader.ReadConfigByKey_Int("incrementSeconds");
    }

    protected override (string Code, string Message)? ValidateOperation() =>
        string.IsNullOrWhiteSpace(Accessor) ? ("HASHICORP_INVALID_CONFIGURATION", "Config key 'accessor' is required for token/renewByAccessor.") : null;

    protected override void AddOperationFields(Dictionary<string, object> d)
    {
        d["accessor"] = Accessor ?? string.Empty;
        if (IncrementSeconds.HasValue) d["incrementSeconds"] = IncrementSeconds.Value;
    }
}
