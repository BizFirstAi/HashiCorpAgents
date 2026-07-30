namespace BizFirst.Integration.HashiCorp.Services.Http.Models;

internal sealed class VaultKvV2VersionMetadata
{
    [JsonPropertyName("version")] public int Version { get; set; }
    [JsonPropertyName("created_time")] public DateTimeOffset CreatedTime { get; set; }
    [JsonPropertyName("deletion_time")] public string? DeletionTime { get; set; }
    [JsonPropertyName("destroyed")] public bool Destroyed { get; set; }
}
