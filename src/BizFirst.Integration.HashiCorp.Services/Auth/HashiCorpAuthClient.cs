using BizFirst.Integration.HashiCorp.Domain;
using BizFirst.Integration.HashiCorp.Services.Http;

namespace BizFirst.Integration.HashiCorp.Services.Auth;

/// <inheritdoc cref="IHashiCorpAuthClient"/>
public sealed class HashiCorpAuthClient : IHashiCorpAuthClient
{
    private readonly HashiCorpApiClient _apiClient;
    private readonly ILogger<HashiCorpAuthClient> _logger;

    public HashiCorpAuthClient(HashiCorpApiClient apiClient, ILogger<HashiCorpAuthClient> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<HashiCorpAppRoleLoginResult> LoginWithAppRoleAsync(
        string vaultAddress,
        string appRolePath,
        string roleID,
        string secretID,
        CancellationToken cancellationToken = default)
    {
        var ctx = new HashiCorpCallContext(vaultAddress, ClientToken: null, Mount: null, EngineVersion: null, Namespace: null);
        var body = new { role_id = roleID, secret_id = secretID };

        try
        {
            using var doc = await _apiClient.SendAuthenticatedAsync(
                ctx, HttpMethod.Post, $"v1/auth/{appRolePath}/login", jsonBody: body, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (!doc.RootElement.TryGetProperty("auth", out var auth) ||
                !auth.TryGetProperty("client_token", out var tokenElement))
            {
                throw new HashiCorpConfigurationException(
                    $"AppRole login at auth/{appRolePath}/login did not return a client_token.");
            }

            var token = tokenElement.GetString();
            if (string.IsNullOrWhiteSpace(token))
                throw new HashiCorpConfigurationException(
                    $"AppRole login at auth/{appRolePath}/login returned an empty client_token.");

            // lease_duration is missing/0 on rare non-standard AppRole configurations — the cache
            // layer treats 0 as "don't cache" rather than guessing a TTL Vault didn't grant.
            var leaseDurationSeconds = auth.TryGetProperty("lease_duration", out var leaseDurationElement)
                && leaseDurationElement.TryGetInt32(out var parsedLeaseDuration)
                    ? parsedLeaseDuration
                    : 0;

            return new HashiCorpAppRoleLoginResult(token, leaseDurationSeconds);
        }
        catch (OperationCanceledException) { throw; }
        catch (HashiCorpApiException ex)
        {
            _logger.LogWarning(
                "[HashiCorpAuthClient] AppRole login failed at auth/{AppRolePath}/login: {StatusCode} {Message}",
                appRolePath, ex.StatusCode, ex.Message);
            // StatusCode 0 is HashiCorpApiException's "no HTTP response at all" sentinel — Vault
            // never had a chance to accept/reject anything, so don't call it "rejected".
            var summary = ex.StatusCode == 0
                ? $"AppRole login could not reach Vault: {ex.Message}"
                : $"AppRole login rejected (HTTP {ex.StatusCode}): {ex.Message}";
            throw new HashiCorpConfigurationException(summary, ex);
        }
    }
}
