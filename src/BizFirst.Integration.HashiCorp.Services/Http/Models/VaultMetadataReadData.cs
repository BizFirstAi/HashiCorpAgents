namespace BizFirst.Integration.HashiCorp.Services.Http.Models;

internal sealed class VaultMetadataReadData
{
    [JsonPropertyName("current_version")] public int CurrentVersion { get; set; }
    [JsonPropertyName("oldest_version")] public int OldestVersion { get; set; }
    [JsonPropertyName("max_versions")] public int MaxVersions { get; set; }
    [JsonPropertyName("cas_required")] public bool CasRequired { get; set; }
    [JsonPropertyName("versions")] public Dictionary<string, VaultMetadataVersionEntry>? Versions { get; set; }
}
