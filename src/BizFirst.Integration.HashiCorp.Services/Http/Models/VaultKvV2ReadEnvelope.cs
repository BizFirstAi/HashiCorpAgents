namespace BizFirst.Integration.HashiCorp.Services.Http.Models;

/// <summary>KV v2 read response envelope: <c>{"data": {"data": {...}, "metadata": {...}}}</c>.</summary>
internal sealed class VaultKvV2ReadEnvelope
{
    [JsonPropertyName("data")] public VaultKvV2ReadData? Data { get; set; }
}
