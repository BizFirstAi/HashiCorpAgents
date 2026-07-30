namespace BizFirst.Integration.HashiCorp.Services.Http.Models;

/// <summary>Token lookup response envelope: <c>{"data": {"accessor": ..., "ttl": N, ...}}</c>.</summary>
internal sealed class VaultTokenLookupEnvelope
{
    [JsonPropertyName("data")] public VaultTokenLookupData? Data { get; set; }
}
