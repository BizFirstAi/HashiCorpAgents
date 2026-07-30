namespace BizFirst.Integration.HashiCorp.Services.Http;

/// <summary>
/// Thrown by <see cref="HashiCorpApiClient"/> when Vault responds with a non-success status this
/// client does not special-case at the call site (e.g. a raw 404 on a Secrets read, a 403, or an
/// unexpected 5xx after retries are exhausted). Carries the HTTP status code and Vault's own
/// <c>errors</c> array so callers can map to the right <c>HASHICORP_*</c> error code without
/// re-parsing the response body themselves.
/// </summary>
/// <remarks>
/// <see cref="StatusCode"/> <c>0</c> is a sentinel meaning "no HTTP response was ever received" —
/// a timeout, DNS failure, connection refused, or TLS failure. It is not a real Vault status code;
/// every resource service's <c>MapErrorCode</c> maps it to <c>HASHICORP_VAULT_UNREACHABLE</c>.
/// </remarks>
public sealed class HashiCorpApiException : Exception
{
    public int StatusCode { get; }
    public IReadOnlyList<string> VaultErrors { get; }

    public HashiCorpApiException(int statusCode, IReadOnlyList<string> vaultErrors)
        : base(BuildMessage(statusCode, vaultErrors))
    {
        StatusCode = statusCode;
        VaultErrors = vaultErrors;
    }

    private static string BuildMessage(int statusCode, IReadOnlyList<string> vaultErrors) =>
        statusCode == 0
            ? vaultErrors.Count > 0 ? vaultErrors[0] : "Vault was unreachable — no HTTP response received."
            : vaultErrors.Count > 0
                ? $"Vault responded {statusCode}: {string.Join("; ", vaultErrors)}"
                : $"Vault responded {statusCode} with no error detail.";
}
