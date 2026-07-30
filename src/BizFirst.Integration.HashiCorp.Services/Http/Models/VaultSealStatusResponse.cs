namespace BizFirst.Integration.HashiCorp.Services.Http.Models;

/// <summary><c>GET /v1/sys/seal-status</c> raw top-level response body.</summary>
internal sealed class VaultSealStatusResponse
{
    [JsonPropertyName("sealed")] public bool Sealed { get; set; }
    [JsonPropertyName("t")] public int T { get; set; }
    [JsonPropertyName("n")] public int N { get; set; }
    [JsonPropertyName("progress")] public int Progress { get; set; }
}
