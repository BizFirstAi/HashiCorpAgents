namespace BizFirst.Integration.HashiCorp.Services.Http.Models;

/// <summary>KV v2 metadata read response: <c>{"data": {"current_version": N, ..., "versions": {"1": {...}}}}</c>.</summary>
internal sealed class VaultMetadataReadEnvelope
{
    [JsonPropertyName("data")] public VaultMetadataReadData? Data { get; set; }
}
