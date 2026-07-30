namespace BizFirst.Integration.HashiCorp.Services.Http.Models;

internal sealed class VaultKvV2ReadData
{
    [JsonPropertyName("data")] public Dictionary<string, string>? Data { get; set; }
    [JsonPropertyName("metadata")] public VaultKvV2VersionMetadata? Metadata { get; set; }
}
