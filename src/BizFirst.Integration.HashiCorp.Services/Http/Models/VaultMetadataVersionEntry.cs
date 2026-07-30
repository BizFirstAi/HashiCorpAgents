namespace BizFirst.Integration.HashiCorp.Services.Http.Models;

internal sealed class VaultMetadataVersionEntry
{
    [JsonPropertyName("created_time")] public DateTimeOffset CreatedTime { get; set; }
    [JsonPropertyName("deletion_time")] public string? DeletionTime { get; set; }
    [JsonPropertyName("destroyed")] public bool Destroyed { get; set; }
}
