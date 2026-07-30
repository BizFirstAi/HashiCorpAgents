namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

/// <summary>LEASE03 — lease/lookup. Checks remaining TTL / renewability before deciding whether to renew.</summary>
internal sealed class LeaseLookupInfo : BaseHashiCorpOperationInfo
{
    public string? LeaseID { get; private set; }

    public override void LoadFrom(ConfigDataPropertyBag reader)
    {
        base.LoadFrom(reader);
        LeaseID = reader.ReadConfigByKeyDefaultNull("leaseID");
    }

    protected override (string Code, string Message)? ValidateOperation() =>
        string.IsNullOrWhiteSpace(LeaseID) ? ("HASHICORP_INVALID_CONFIGURATION", "Config key 'leaseID' is required for lease/lookup.") : null;

    protected override void AddOperationFields(Dictionary<string, object> d)
    {
        d["leaseID"] = LeaseID ?? string.Empty;
    }
}
