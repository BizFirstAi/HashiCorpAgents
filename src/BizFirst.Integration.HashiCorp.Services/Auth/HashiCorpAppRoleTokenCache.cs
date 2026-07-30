using System.Collections.Concurrent;

namespace BizFirst.Integration.HashiCorp.Services.Auth;

/// <inheritdoc cref="IHashiCorpAppRoleTokenCache"/>
/// <remarks>
/// Registered as a singleton (see <c>HashiCorpServicesExtensions</c>) so the cache outlives the
/// scoped <c>HashiCorpCredentialResolver</c> that consumes it — a per-execution cache would never
/// produce a hit. A per-credentialID <see cref="SemaphoreSlim"/> makes concurrent executions that
/// share a credential single-flight through one login rather than each consuming a <c>secret_id</c>.
/// </remarks>
public sealed class HashiCorpAppRoleTokenCache : IHashiCorpAppRoleTokenCache
{
    private sealed record CachedToken(string ClientToken, DateTimeOffset ExpiresAtUtc);

    // Renew this many seconds before Vault's actual expiry, so a token already in flight to a
    // resource service never expires mid-call.
    private const int ExpirySafetyBufferSeconds = 30;

    private readonly ConcurrentDictionary<int, CachedToken> _cache = new();
    private readonly ConcurrentDictionary<int, SemaphoreSlim> _locks = new();

    public async Task<string> GetOrCreateAsync(
        int credentialID,
        Func<CancellationToken, Task<HashiCorpAppRoleLoginResult>> loginFactory,
        CancellationToken cancellationToken)
    {
        if (TryGetValid(credentialID, out var cachedToken))
            return cachedToken;

        var gate = _locks.GetOrAdd(credentialID, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Re-check: another caller may have already refreshed this credentialID while we waited.
            if (TryGetValid(credentialID, out cachedToken))
                return cachedToken;

            var login = await loginFactory(cancellationToken).ConfigureAwait(false);

            // A non-positive (or too-small) lease_duration means Vault didn't grant a TTL worth
            // caching against — return the token without caching so the next call re-logs in rather
            // than risk caching something with no reliable expiry.
            if (login.LeaseDurationSeconds > ExpirySafetyBufferSeconds)
            {
                var expiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(login.LeaseDurationSeconds - ExpirySafetyBufferSeconds);
                _cache[credentialID] = new CachedToken(login.ClientToken, expiresAtUtc);
            }

            return login.ClientToken;
        }
        finally
        {
            gate.Release();
        }
    }

    private bool TryGetValid(int credentialID, out string clientToken)
    {
        if (_cache.TryGetValue(credentialID, out var cached) && cached.ExpiresAtUtc > DateTimeOffset.UtcNow)
        {
            clientToken = cached.ClientToken;
            return true;
        }

        clientToken = string.Empty;
        return false;
    }
}
