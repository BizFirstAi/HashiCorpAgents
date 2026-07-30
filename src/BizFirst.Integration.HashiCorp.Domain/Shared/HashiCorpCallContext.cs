namespace BizFirst.Integration.HashiCorp.Domain;

/// <summary>
/// Per-call context passed into every <c>BizFirst.Integration.HashiCorp.Services</c> method instead
/// of each method taking loose parameters. Keeps the resolved client token out of individual method
/// signatures and makes it obvious that no service method reaches into credential resolution itself
/// — only the executor layer does that.
/// </summary>
/// <param name="VaultAddress">The tenant's Vault server base URL, e.g. <c>https://vault.internal.example.com:8200</c>. No BizFirst-wide default exists.</param>
/// <param name="ClientToken">The resolved Vault client token used as the <c>X-Vault-Token</c> header. Null/empty for the unauthenticated System (health/seal-status) endpoints.</param>
/// <param name="Mount">KV engine mount path, e.g. <c>"secret"</c>. Null for Token/Lease/System operations, which are not KV-scoped.</param>
/// <param name="EngineVersion">KV engine version, <c>"1"</c> or <c>"2"</c>. Null for Token/Lease/System operations.</param>
/// <param name="Namespace">Optional Vault Enterprise namespace, sent as <c>X-Vault-Namespace</c> when present.</param>
public sealed record HashiCorpCallContext(
    string  VaultAddress,
    string? ClientToken,
    string? Mount,
    string? EngineVersion,
    string? Namespace);
