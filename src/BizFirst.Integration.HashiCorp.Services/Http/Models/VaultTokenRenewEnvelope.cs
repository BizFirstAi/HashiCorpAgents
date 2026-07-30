namespace BizFirst.Integration.HashiCorp.Services.Http.Models;

/// <summary>Token renew response envelope: <c>{"auth": {"client_token": ..., "lease_duration": N, ...}}</c>.</summary>
internal sealed class VaultTokenRenewEnvelope
{
    [JsonPropertyName("auth")] public VaultTokenRenewAuth? Auth { get; set; }
}
