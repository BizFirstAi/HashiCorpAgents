namespace BizFirst.Integration.HashiCorp.Services.Auth;

/// <summary>AppRole login exchange — used only by <see cref="IHashiCorpCredentialResolver"/>.</summary>
public interface IHashiCorpAuthClient
{
    /// <summary>
    /// Exchanges an AppRole role-id/secret-id pair for a client token via
    /// <c>POST /v1/auth/{appRolePath}/login</c>.
    /// </summary>
    /// <param name="vaultAddress">The tenant's Vault server base URL.</param>
    /// <param name="appRolePath">Vault auth-method mount path, e.g. <c>"approle"</c> or a tenant's custom mount.</param>
    /// <param name="roleID">The AppRole's <c>role_id</c>.</param>
    /// <param name="secretID">The AppRole's <c>secret_id</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<HashiCorpAppRoleLoginResult> LoginWithAppRoleAsync(
        string vaultAddress,
        string appRolePath,
        string roleID,
        string secretID,
        CancellationToken cancellationToken = default);
}
