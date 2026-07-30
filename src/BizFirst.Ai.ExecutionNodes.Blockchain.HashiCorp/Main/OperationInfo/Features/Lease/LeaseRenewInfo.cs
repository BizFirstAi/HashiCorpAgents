namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

/// <summary>LEASE01 — lease/renew. Extends a lease on a dynamic secret issued earlier in the workflow.</summary>
internal sealed class LeaseRenewInfo : BaseHashiCorpOperationInfo
{
    public string? LeaseID { get; private set; }
    public int? IncrementSeconds { get; private set; }

    public override void LoadFrom(ConfigDataPropertyBag reader)
    {
        base.LoadFrom(reader);
        LeaseID = reader.ReadConfigByKeyDefaultNull("leaseID");
        IncrementSeconds = reader.ReadConfigByKey_Int("incrementSeconds");
    }

    protected override (string Code, string Message)? ValidateOperation() =>
        string.IsNullOrWhiteSpace(LeaseID) ? ("HASHICORP_INVALID_CONFIGURATION", "Config key 'leaseID' is required for lease/renew.") : null;

    protected override void AddOperationFields(Dictionary<string, object> d)
    {
        d["leaseID"] = LeaseID ?? string.Empty;
        if (IncrementSeconds.HasValue) d["incrementSeconds"] = IncrementSeconds.Value;
    }
}
