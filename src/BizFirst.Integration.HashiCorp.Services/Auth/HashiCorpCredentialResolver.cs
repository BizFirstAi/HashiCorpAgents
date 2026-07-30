using BizFirst.Ai.ProcessEngine.Domain.Credentials;

namespace BizFirst.Integration.HashiCorp.Services.Auth;

/// <inheritdoc cref="IHashiCorpCredentialResolver"/>
public sealed class HashiCorpCredentialResolver : IHashiCorpCredentialResolver
{
    private readonly ICredentialResolver _credentialResolver;
    private readonly IHashiCorpAuthClient _authClient;
    private readonly IHashiCorpAppRoleTokenCache _tokenCache;

    public HashiCorpCredentialResolver(
        ICredentialResolver credentialResolver, IHashiCorpAuthClient authClient, IHashiCorpAppRoleTokenCache tokenCache)
    {
        _credentialResolver = credentialResolver ?? throw new ArgumentNullException(nameof(credentialResolver));
        _authClient = authClient ?? throw new ArgumentNullException(nameof(authClient));
        _tokenCache = tokenCache ?? throw new ArgumentNullException(nameof(tokenCache));
    }

    /// <inheritdoc/>
    public async Task<string> ResolveClientTokenAsync(
        int credentialID,
        string authMethod,
        string vaultAddress,
        string? appRolePath = null,
        CancellationToken cancellationToken = default)
    {
        switch (authMethod)
        {
            case "token":
            {
                var token = await _credentialResolver.GetBearerTokenAsync(credentialID, cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(token))
                    throw new HashiCorpConfigurationException(
                        $"Credential {credentialID} did not resolve to a usable Vault token.");
                return token;
            }

            case "appRole":
            {
                var pair = await _credentialResolver.GetPasswordAsync(credentialID, cancellationToken).ConfigureAwait(false);
                if (pair is null || string.IsNullOrWhiteSpace(pair.Password))
                    throw new HashiCorpConfigurationException(
                        $"Credential {credentialID} did not resolve to a usable AppRole RoleID/SecretID pair.");

                // pair.Username = RoleID, pair.Password = SecretID, by this node's own convention —
                // not a field that exists natively on PasswordRecord; documented here so the mapping
                // is never re-derived or re-guessed by a future implementer.
                var resolvedAppRolePath = string.IsNullOrWhiteSpace(appRolePath) ? "approle" : appRolePath;
                var roleID = pair.Username ?? string.Empty;
                var secretID = pair.Password;

                // Cached by credentialID so a single-use secret_id is consumed at most once per TTL
                // window instead of on every execution — see IHashiCorpAppRoleTokenCache.
                return await _tokenCache.GetOrCreateAsync(
                    credentialID,
                    ct => _authClient.LoginWithAppRoleAsync(vaultAddress, resolvedAppRolePath, roleID, secretID, ct),
                    cancellationToken)
                    .ConfigureAwait(false);
            }

            default:
                throw new HashiCorpConfigurationException($"Unknown authMethod '{authMethod}'.");
        }
    }
}
