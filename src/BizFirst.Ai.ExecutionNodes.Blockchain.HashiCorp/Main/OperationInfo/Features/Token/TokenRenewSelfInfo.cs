namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

/// <summary>TOK03 — token/renewSelf. Extends this token's TTL before it expires mid-workflow.</summary>
internal sealed class TokenRenewSelfInfo : BaseHashiCorpOperationInfo
{
    public int? IncrementSeconds { get; private set; }

    public override void LoadFrom(ConfigDataPropertyBag reader)
    {
        base.LoadFrom(reader);
        IncrementSeconds = reader.ReadConfigByKey_Int("incrementSeconds");
    }

    protected override void AddOperationFields(Dictionary<string, object> d)
    {
        if (IncrementSeconds.HasValue) d["incrementSeconds"] = IncrementSeconds.Value;
    }
}
