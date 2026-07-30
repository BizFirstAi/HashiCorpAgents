namespace BizFirst.Integration.HashiCorp.Services.Auth;

/// <summary>
/// Caches AppRole-derived Vault client tokens across executions, keyed by the BizFirst
/// <c>credentialID</c> the RoleID/SecretID pair was resolved from. Avoids re-logging-in (and
/// re-consuming a possibly single-use <c>secret_id</c>) on every node execution when the
/// previously-issued token is still within its Vault-granted TTL.
/// </summary>
public interface IHashiCorpAppRoleTokenCache
{
    /// <summary>
    /// Returns a cached, still-valid client token for <paramref name="credentialID"/> if one exists;
    /// otherwise calls <paramref name="loginFactory"/> exactly once (concurrent callers for the same
    /// <paramref name="credentialID"/> share the same in-flight login rather than each consuming a
    /// separate <c>secret_id</c>), caches the result per its granted TTL, and returns it.
    /// </summary>
    Task<string> GetOrCreateAsync(
        int credentialID,
        Func<CancellationToken, Task<HashiCorpAppRoleLoginResult>> loginFactory,
        CancellationToken cancellationToken);
}
