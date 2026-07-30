namespace BizFirst.Integration.HashiCorp.Services.Http.Models;

/// <summary>Lease renew response: <c>{"lease_id": ..., "lease_duration": N, "renewable": bool}</c> (top-level, not under "data").</summary>
internal sealed class VaultLeaseRenewResponse
{
    [JsonPropertyName("lease_id")] public string LeaseID { get; set; } = string.Empty;
    [JsonPropertyName("lease_duration")] public int LeaseDuration { get; set; }
    [JsonPropertyName("renewable")] public bool Renewable { get; set; }
}
