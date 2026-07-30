namespace BizFirst.Integration.HashiCorp.Services.Http.Models;

/// <summary>KV v1 read response envelope: <c>{"data": {...arbitrary secret fields...}}</c>.</summary>
internal sealed class VaultKvV1ReadEnvelope
{
    [JsonPropertyName("data")] public Dictionary<string, string>? Data { get; set; }
}
