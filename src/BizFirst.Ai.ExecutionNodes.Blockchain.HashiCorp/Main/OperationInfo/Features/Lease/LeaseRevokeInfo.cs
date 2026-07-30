namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

/// <summary>LEASE02 — lease/revoke. Immediately invalidates a lease (e.g. after a workflow finishes using a dynamic DB credential).</summary>
internal sealed class LeaseRevokeInfo : BaseHashiCorpOperationInfo
{
    public string? LeaseID { get; private set; }

    public override void LoadFrom(ConfigDataPropertyBag reader)
    {
        base.LoadFrom(reader);
        LeaseID = reader.ReadConfigByKeyDefaultNull("leaseID");
    }

    protected override (string Code, string Message)? ValidateOperation() =>
        string.IsNullOrWhiteSpace(LeaseID) ? ("HASHICORP_INVALID_CONFIGURATION", "Config key 'leaseID' is required for lease/revoke.") : null;

    protected override void AddOperationFields(Dictionary<string, object> d)
    {
        d["leaseID"] = LeaseID ?? string.Empty;
    }
}
