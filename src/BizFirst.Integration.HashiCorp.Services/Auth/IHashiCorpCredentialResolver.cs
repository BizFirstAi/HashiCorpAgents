namespace BizFirst.Integration.HashiCorp.Services.Auth;

/// <summary>
/// Resolves the node's own Vault authentication material — a Vault token, or an AppRole pair —
/// to a usable client token. Never resolves secrets that live inside Vault; that is the entire
/// job of <c>HashiCorpSecretService</c>, and none of that data flows through this interface.
/// </summary>
public interface IHashiCorpCredentialResolver
{
    /// <param name="credentialID">BizFirst credential record ID.</param>
    /// <param name="authMethod"><c>"token"</c> or <c>"appRole"</c>.</param>
    /// <param name="vaultAddress">The tenant's Vault server base URL.</param>
    /// <param name="appRolePath">Vault auth-method mount path; only used when <paramref name="authMethod"/> is <c>"appRole"</c>. Defaults to <c>"approle"</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<string> ResolveClientTokenAsync(
        int credentialID,
        string authMethod,
        string vaultAddress,
        string? appRolePath = null,
        CancellationToken cancellationToken = default);
}
