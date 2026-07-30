namespace BizFirst.Integration.HashiCorp.Services.Auth;

/// <summary>
/// Result of a successful AppRole login — the client token plus the TTL Vault granted it, so the
/// caller can cache the token instead of re-logging-in (and re-consuming a possibly single-use
/// <c>secret_id</c>) on every execution.
/// </summary>
/// <param name="ClientToken">The Vault client token to use for subsequent authenticated calls.</param>
/// <param name="LeaseDurationSeconds">
/// Vault's <c>auth.lease_duration</c>, in seconds. 0 if Vault omitted the field — callers should
/// treat that as "unknown TTL, don't cache" rather than assume a default.
/// </param>
public sealed record HashiCorpAppRoleLoginResult(string ClientToken, int LeaseDurationSeconds);
