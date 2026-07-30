namespace BizFirst.Integration.HashiCorp.Services.Http.Models;

/// <summary>Lease lookup response envelope: <c>{"data": {"id": ..., "ttl": N, "renewable": bool, ...}}</c>.</summary>
internal sealed class VaultLeaseLookupEnvelope
{
    [JsonPropertyName("data")] public VaultLeaseLookupData? Data { get; set; }
}
