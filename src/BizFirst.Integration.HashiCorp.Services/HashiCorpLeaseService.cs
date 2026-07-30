using BizFirst.Integration.HashiCorp.Domain;
using BizFirst.Integration.HashiCorp.Services.Http;
using BizFirst.Integration.HashiCorp.Services.Http.Models;

namespace BizFirst.Integration.HashiCorp.Services;

/// <summary>
/// Lease operations (LEASE01–LEASE03) — managing leases on dynamic secrets issued earlier in a
/// workflow by some other engine (database, AWS, PKI). This service does not itself provide a way
/// to create a lease; it only renews/revokes/inspects one a workflow already holds the ID for.
/// </summary>
public sealed class HashiCorpLeaseService
{
    private readonly HashiCorpApiClient _apiClient;
    private readonly ILogger<HashiCorpLeaseService> _logger;

    public HashiCorpLeaseService(HashiCorpApiClient apiClient, ILogger<HashiCorpLeaseService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>LEASE01 — Renew Lease. Extends a lease on a dynamic secret (e.g. a database credential Vault issued earlier in the workflow).</summary>
    public async Task<HashiCorpLeaseRenewResult> RenewAsync(
        HashiCorpCallContext ctx, string leaseID, int? incrementSeconds, CancellationToken cancellationToken = default)
    {
        try
        {
            object body = incrementSeconds.HasValue
                ? new { lease_id = leaseID, increment = incrementSeconds.Value }
                : new { lease_id = leaseID };

            using var doc = await _apiClient.SendAuthenticatedAsync(
                ctx, HttpMethod.Put, "v1/sys/leases/renew", jsonBody: body, cancellationToken: cancellationToken).ConfigureAwait(false);

            var response = doc.RootElement.Deserialize<VaultLeaseRenewResponse>();
            return response is null
                ? HashiCorpLeaseRenewResult.Fail(leaseID, "HASHICORP_UPSTREAM_ERROR", "Vault returned no lease data on renew.")
                : HashiCorpLeaseRenewResult.Ok(response.LeaseID, response.LeaseDuration, response.Renewable);
        }
        catch (OperationCanceledException) { throw; }
        catch (HashiCorpApiException ex)
        {
            _logger.LogWarning(ex, "[HashiCorpLeaseService] Renew failed for {LeaseID}", leaseID);
            return HashiCorpLeaseRenewResult.Fail(leaseID, MapErrorCode(ex, isRenew: true), ex.Message);
        }
    }

    /// <summary>LEASE02 — Revoke Lease. Immediately invalidates a lease (e.g. after a workflow finishes using a dynamic DB credential).</summary>
    public async Task<HashiCorpLeaseRevokeResult> RevokeAsync(
        HashiCorpCallContext ctx, string leaseID, CancellationToken cancellationToken = default)
    {
        try
        {
            using var _ = await _apiClient.SendAuthenticatedAsync(
                ctx, HttpMethod.Put, "v1/sys/leases/revoke", jsonBody: new { lease_id = leaseID }, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return HashiCorpLeaseRevokeResult.Ok(leaseID);
        }
        catch (OperationCanceledException) { throw; }
        catch (HashiCorpApiException ex)
        {
            _logger.LogWarning(ex, "[HashiCorpLeaseService] Revoke failed for {LeaseID}", leaseID);
            return HashiCorpLeaseRevokeResult.Fail(leaseID, MapErrorCode(ex, isRenew: false), ex.Message);
        }
    }

    /// <summary>LEASE03 — Lookup Lease. Checks remaining TTL / renewability before deciding whether to renew.</summary>
    public async Task<HashiCorpLeaseLookupResult> LookupAsync(
        HashiCorpCallContext ctx, string leaseID, CancellationToken cancellationToken = default)
    {
        try
        {
            using var doc = await _apiClient.SendAuthenticatedAsync(
                ctx, HttpMethod.Put, "v1/sys/leases/lookup", jsonBody: new { lease_id = leaseID }, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var data = doc.RootElement.Deserialize<VaultLeaseLookupEnvelope>()?.Data;
            return data is null
                ? HashiCorpLeaseLookupResult.Fail(leaseID, "HASHICORP_UPSTREAM_ERROR", "Vault returned no lease data on lookup.")
                : HashiCorpLeaseLookupResult.Ok(string.IsNullOrEmpty(data.Id) ? leaseID : data.Id, data.Ttl, data.Renewable);
        }
        catch (OperationCanceledException) { throw; }
        catch (HashiCorpApiException ex)
        {
            _logger.LogWarning(ex, "[HashiCorpLeaseService] Lookup failed for {LeaseID}", leaseID);
            return HashiCorpLeaseLookupResult.Fail(leaseID, MapErrorCode(ex, isRenew: false), ex.Message);
        }
    }

    // ── Shared error mapping ─────────────────────────────────────────────────

    internal static string MapErrorCode(HashiCorpApiException ex, bool isRenew) => ex.StatusCode switch
    {
        0 => "HASHICORP_VAULT_UNREACHABLE", // sentinel: no HTTP response at all — timeout/DNS/connection failure
        503 => "HASHICORP_SEALED",
        400 or 404 when isRenew && ex.VaultErrors.Any(e => e.Contains("not renewable", StringComparison.OrdinalIgnoreCase))
            => "HASHICORP_LEASE_NOT_RENEWABLE",
        400 or 404 => "HASHICORP_LEASE_NOT_FOUND",
        403 => "HASHICORP_PERMISSION_DENIED",
        >= 500 and < 600 => "HASHICORP_UPSTREAM_ERROR",
        _ => "HASHICORP_UPSTREAM_ERROR",
    };
}
