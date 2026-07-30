using BizFirst.Integration.HashiCorp.Domain;
using BizFirst.Integration.HashiCorp.Services.Http;
using BizFirst.Integration.HashiCorp.Services.Http.Models;

namespace BizFirst.Integration.HashiCorp.Services;

/// <summary>
/// Token operations (TOK01–TOK05) — deliberately scoped to what an automation credential should
/// reasonably do. No <c>token/create</c> for arbitrary new tokens with elevated policies; that is
/// an admin operation, not a workflow-automation one.
/// </summary>
public sealed class HashiCorpTokenService
{
    private readonly HashiCorpApiClient _apiClient;
    private readonly ILogger<HashiCorpTokenService> _logger;

    public HashiCorpTokenService(HashiCorpApiClient apiClient, ILogger<HashiCorpTokenService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>TOK01 — Lookup Self. Returns this token's TTL, policies, renewable flag. No path parameter, mutates nothing.</summary>
    public async Task<HashiCorpTokenLookupSelfResult> LookupSelfAsync(HashiCorpCallContext ctx, CancellationToken cancellationToken = default)
    {
        try
        {
            using var doc = await _apiClient.SendAuthenticatedAsync(
                ctx, HttpMethod.Get, "v1/auth/token/lookup-self", cancellationToken: cancellationToken).ConfigureAwait(false);

            var info = ToTokenInfo(doc.RootElement.Deserialize<VaultTokenLookupEnvelope>()?.Data);
            return info is null
                ? HashiCorpTokenLookupSelfResult.Fail("HASHICORP_UPSTREAM_ERROR", "Vault returned no token data.")
                : HashiCorpTokenLookupSelfResult.Ok(info);
        }
        catch (OperationCanceledException) { throw; }
        catch (HashiCorpApiException ex)
        {
            _logger.LogWarning(ex, "[HashiCorpTokenService] LookupSelf failed");
            return HashiCorpTokenLookupSelfResult.Fail(MapErrorCode(ex), ex.Message);
        }
    }

    /// <summary>TOK02 — Lookup by Accessor. Inspects a different token (e.g. a child token minted earlier) without needing its raw value.</summary>
    public async Task<HashiCorpTokenLookupByAccessorResult> LookupByAccessorAsync(
        HashiCorpCallContext ctx, string accessor, CancellationToken cancellationToken = default)
    {
        try
        {
            using var doc = await _apiClient.SendAuthenticatedAsync(
                ctx, HttpMethod.Post, "v1/auth/token/lookup-accessor", jsonBody: new { accessor }, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var info = ToTokenInfo(doc.RootElement.Deserialize<VaultTokenLookupEnvelope>()?.Data);
            return info is null
                ? HashiCorpTokenLookupByAccessorResult.Fail("HASHICORP_UPSTREAM_ERROR", "Vault returned no token data.")
                : HashiCorpTokenLookupByAccessorResult.Ok(info);
        }
        catch (OperationCanceledException) { throw; }
        catch (HashiCorpApiException ex)
        {
            _logger.LogWarning(ex, "[HashiCorpTokenService] LookupByAccessor failed for {Accessor}", accessor);
            return HashiCorpTokenLookupByAccessorResult.Fail(MapErrorCode(ex), ex.Message);
        }
    }

    /// <summary>TOK03 — Renew Self. Extends this token's TTL before it expires mid-workflow.</summary>
    public async Task<HashiCorpTokenRenewSelfResult> RenewSelfAsync(
        HashiCorpCallContext ctx, int? incrementSeconds, CancellationToken cancellationToken = default)
    {
        try
        {
            object body = incrementSeconds.HasValue ? new { increment = incrementSeconds.Value } : new { };
            using var doc = await _apiClient.SendAuthenticatedAsync(
                ctx, HttpMethod.Post, "v1/auth/token/renew-self", jsonBody: body, cancellationToken: cancellationToken).ConfigureAwait(false);

            var auth = doc.RootElement.Deserialize<VaultTokenRenewEnvelope>()?.Auth;
            return auth is null
                ? HashiCorpTokenRenewSelfResult.Fail("HASHICORP_UPSTREAM_ERROR", "Vault returned no auth block on renew.")
                : HashiCorpTokenRenewSelfResult.Ok(auth.Accessor ?? string.Empty, auth.LeaseDuration);
        }
        catch (OperationCanceledException) { throw; }
        catch (HashiCorpApiException ex)
        {
            _logger.LogWarning(ex, "[HashiCorpTokenService] RenewSelf failed");
            return HashiCorpTokenRenewSelfResult.Fail(MapErrorCode(ex, renewFailure: true), ex.Message);
        }
    }

    /// <summary>TOK04 — Renew Token by Accessor. Extends a delegated/child token's TTL.</summary>
    public async Task<HashiCorpTokenRenewByAccessorResult> RenewByAccessorAsync(
        HashiCorpCallContext ctx, string accessor, int? incrementSeconds, CancellationToken cancellationToken = default)
    {
        try
        {
            object body = incrementSeconds.HasValue
                ? new { accessor, increment = incrementSeconds.Value }
                : new { accessor };

            using var doc = await _apiClient.SendAuthenticatedAsync(
                ctx, HttpMethod.Post, "v1/auth/token/renew-accessor", jsonBody: body, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var auth = doc.RootElement.Deserialize<VaultTokenRenewEnvelope>()?.Auth;
            return auth is null
                ? HashiCorpTokenRenewByAccessorResult.Fail("HASHICORP_UPSTREAM_ERROR", "Vault returned no auth block on renew.")
                : HashiCorpTokenRenewByAccessorResult.Ok(auth.Accessor ?? accessor, auth.LeaseDuration);
        }
        catch (OperationCanceledException) { throw; }
        catch (HashiCorpApiException ex)
        {
            _logger.LogWarning(ex, "[HashiCorpTokenService] RenewByAccessor failed for {Accessor}", accessor);
            return HashiCorpTokenRenewByAccessorResult.Fail(MapErrorCode(ex, renewFailure: true), ex.Message);
        }
    }

    /// <summary>TOK05 — Revoke Token by Accessor. Cleans up a short-lived child token without needing to hold its raw value.</summary>
    public async Task<HashiCorpTokenRevokeByAccessorResult> RevokeByAccessorAsync(
        HashiCorpCallContext ctx, string accessor, CancellationToken cancellationToken = default)
    {
        try
        {
            using var _ = await _apiClient.SendAuthenticatedAsync(
                ctx, HttpMethod.Post, "v1/auth/token/revoke-accessor", jsonBody: new { accessor }, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return HashiCorpTokenRevokeByAccessorResult.Ok(accessor);
        }
        catch (OperationCanceledException) { throw; }
        catch (HashiCorpApiException ex)
        {
            _logger.LogWarning(ex, "[HashiCorpTokenService] RevokeByAccessor failed for {Accessor}", accessor);
            return HashiCorpTokenRevokeByAccessorResult.Fail(accessor, MapErrorCode(ex), ex.Message);
        }
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    private static HashiCorpTokenInfo? ToTokenInfo(VaultTokenLookupData? data) =>
        data is null
            ? null
            : new HashiCorpTokenInfo(data.Accessor, data.Ttl, data.Renewable, data.Policies ?? [], data.DisplayName ?? string.Empty);

    internal static string MapErrorCode(HashiCorpApiException ex, bool renewFailure = false) => ex.StatusCode switch
    {
        0 => "HASHICORP_VAULT_UNREACHABLE", // sentinel: no HTTP response at all — timeout/DNS/connection failure
        503 => "HASHICORP_SEALED",
        403 when ex.VaultErrors.Any(e => e.Contains("expired", StringComparison.OrdinalIgnoreCase)) => "HASHICORP_TOKEN_EXPIRED",
        403 when renewFailure => "HASHICORP_LEASE_NOT_RENEWABLE",
        403 => "HASHICORP_PERMISSION_DENIED",
        400 when renewFailure => "HASHICORP_LEASE_NOT_RENEWABLE",
        >= 500 and < 600 => "HASHICORP_UPSTREAM_ERROR",
        _ => "HASHICORP_AUTH_FAILED",
    };
}
