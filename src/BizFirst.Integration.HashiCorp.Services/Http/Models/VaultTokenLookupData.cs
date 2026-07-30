namespace BizFirst.Integration.HashiCorp.Services.Http.Models;

internal sealed class VaultTokenLookupData
{
    [JsonPropertyName("accessor")] public string Accessor { get; set; } = string.Empty;
    [JsonPropertyName("ttl")] public int Ttl { get; set; }
    [JsonPropertyName("renewable")] public bool Renewable { get; set; }
    [JsonPropertyName("policies")] public List<string>? Policies { get; set; }
    [JsonPropertyName("display_name")] public string? DisplayName { get; set; }
}
