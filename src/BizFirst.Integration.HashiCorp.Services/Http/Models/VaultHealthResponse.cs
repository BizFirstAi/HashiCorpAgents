namespace BizFirst.Integration.HashiCorp.Services.Http.Models;

/// <summary><c>GET /v1/sys/health</c> raw top-level response body.</summary>
internal sealed class VaultHealthResponse
{
    [JsonPropertyName("initialized")] public bool Initialized { get; set; }
    [JsonPropertyName("sealed")] public bool Sealed { get; set; }
    [JsonPropertyName("standby")] public bool Standby { get; set; }
    [JsonPropertyName("version")] public string? Version { get; set; }
    [JsonPropertyName("cluster_name")] public string? ClusterName { get; set; }
}
