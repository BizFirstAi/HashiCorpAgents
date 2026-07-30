namespace BizFirst.Integration.HashiCorp.Services.Http.Models;

internal sealed class VaultLeaseLookupData
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("ttl")] public int Ttl { get; set; }
    [JsonPropertyName("renewable")] public bool Renewable { get; set; }
}
