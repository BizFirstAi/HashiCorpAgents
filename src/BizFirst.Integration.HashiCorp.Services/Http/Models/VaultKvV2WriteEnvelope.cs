namespace BizFirst.Integration.HashiCorp.Services.Http.Models;

/// <summary>KV v2 write response envelope: <c>{"data": {"version": N, ...}}</c>.</summary>
internal sealed class VaultKvV2WriteEnvelope
{
    [JsonPropertyName("data")] public VaultKvV2VersionMetadata? Data { get; set; }
}
