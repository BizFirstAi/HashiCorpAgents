namespace BizFirst.Integration.HashiCorp.Services.Http.Models;

internal sealed class VaultTokenRenewAuth
{
    [JsonPropertyName("accessor")] public string? Accessor { get; set; }
    [JsonPropertyName("lease_duration")] public int LeaseDuration { get; set; }
    [JsonPropertyName("renewable")] public bool Renewable { get; set; }
}
