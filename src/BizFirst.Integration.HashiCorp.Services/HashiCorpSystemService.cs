using BizFirst.Integration.HashiCorp.Domain;
using BizFirst.Integration.HashiCorp.Services.Http;
using BizFirst.Integration.HashiCorp.Services.Http.Models;

namespace BizFirst.Integration.HashiCorp.Services;

/// <summary>
/// Unauthenticated pre-flight checks (SYS01–SYS02). <c>/v1/sys/health</c>'s HTTP status code encodes
/// cluster role (200 active, 429 standby, 472 DR secondary, 473 performance standby, 474
/// standby-unreachable, 501 uninitialized, 503 sealed, 530 removed) rather than success/failure —
/// this service normalizes every documented code into a successful result carrying the decoded
/// state, and defaults to <c>standbyok=true&amp;perfstandbyok=true</c> since SYS01's purpose here is
/// "is any node in the cluster reachable and unsealed," not "is this address the active leader."
/// Only a genuine network failure produces <see cref="HashiCorpSystemHealthResult.Fail"/>.
/// </summary>
public sealed class HashiCorpSystemService
{
    private readonly HashiCorpApiClient _apiClient;
    private readonly ILogger<HashiCorpSystemService> _logger;

    public HashiCorpSystemService(HashiCorpApiClient apiClient, ILogger<HashiCorpSystemService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>SYS01 — Health Check.</summary>
    public async Task<HashiCorpSystemHealthResult> HealthAsync(string vaultAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new Dictionary<string, string> { ["standbyok"] = "true", ["perfstandbyok"] = "true" };
            var (statusCode, doc) = await _apiClient.SendUnauthenticatedRawAsync(
                vaultAddress, "v1/sys/health", query, cancellationToken).ConfigureAwait(false);

            using (doc)
            {
                var body = doc.RootElement.Deserialize<VaultHealthResponse>() ?? new VaultHealthResponse();
                return HashiCorpSystemHealthResult.Ok(
                    body.Initialized, body.Sealed, body.Standby, body.Version ?? string.Empty, body.ClusterName ?? string.Empty, statusCode);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (HashiCorpApiException ex)
        {
            // HashiCorpApiClient wraps every genuine (non-cancellation) network failure or timeout on the
            // unauthenticated path as HashiCorpApiException(0, ...) — the same sentinel used on the
            // authenticated path — so this is the only catch needed here.
            _logger.LogError(ex, "[HashiCorpSystemService] Health check could not reach {VaultAddress}", vaultAddress);
            return HashiCorpSystemHealthResult.Fail("HASHICORP_VAULT_UNREACHABLE", ex.Message);
        }
    }

    /// <summary>SYS02 — Seal Status. Answers even when Vault is sealed — a sealed Vault answers every other call with 503.</summary>
    public async Task<HashiCorpSystemSealStatusResult> SealStatusAsync(string vaultAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            var (_, doc) = await _apiClient.SendUnauthenticatedRawAsync(
                vaultAddress, "v1/sys/seal-status", cancellationToken: cancellationToken).ConfigureAwait(false);

            using (doc)
            {
                var body = doc.RootElement.Deserialize<VaultSealStatusResponse>() ?? new VaultSealStatusResponse();
                return HashiCorpSystemSealStatusResult.Ok(body.Sealed, body.T, body.N, body.Progress);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (HashiCorpApiException ex)
        {
            // HashiCorpApiClient wraps every genuine (non-cancellation) network failure or timeout on the
            // unauthenticated path as HashiCorpApiException(0, ...) — the same sentinel used on the
            // authenticated path — so this is the only catch needed here.
            _logger.LogError(ex, "[HashiCorpSystemService] Seal status could not reach {VaultAddress}", vaultAddress);
            return HashiCorpSystemSealStatusResult.Fail("HASHICORP_VAULT_UNREACHABLE", ex.Message);
        }
    }
}
