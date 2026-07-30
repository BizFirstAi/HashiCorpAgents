using BizFirst.Integration.HashiCorp.Domain;
using BizFirst.Integration.HashiCorp.Services.Http;
using BizFirst.Integration.HashiCorp.Services.Http.Models;

namespace BizFirst.Integration.HashiCorp.Services;

/// <summary>
/// KV v1/v2 secrets operations (SEC01–SEC08) — the reason this node exists. Every method branches
/// on <see cref="HashiCorpCallContext.EngineVersion"/> because v1 has no versioning/soft-delete
/// concept, so SEC04/SEC05/SEC07/SEC08 have no v1 equivalent at all.
/// </summary>
public sealed class HashiCorpSecretService
{
    private readonly HashiCorpApiClient _apiClient;
    private readonly ILogger<HashiCorpSecretService> _logger;

    public HashiCorpSecretService(HashiCorpApiClient apiClient, ILogger<HashiCorpSecretService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private static bool IsV2(HashiCorpCallContext ctx) => ctx.EngineVersion != "1";

    /// <summary>SEC01 — Read Secret.</summary>
    public async Task<HashiCorpSecretsReadResult> ReadAsync(
        HashiCorpCallContext ctx, string path, int? version, CancellationToken cancellationToken = default)
    {
        try
        {
            if (IsV2(ctx))
            {
                var query = version is > 0 ? new Dictionary<string, string> { ["version"] = version.Value.ToString() } : null;
                using var doc = await _apiClient.SendAuthenticatedAsync(
                    ctx, HttpMethod.Get, $"v1/{ctx.Mount}/data/{path}", query, cancellationToken: cancellationToken).ConfigureAwait(false);

                var envelope = doc.RootElement.Deserialize<VaultKvV2ReadEnvelope>();
                var data = envelope?.Data?.Data ?? new Dictionary<string, string>();
                var resolvedVersion = envelope?.Data?.Metadata?.Version ?? 0;
                return HashiCorpSecretsReadResult.Ok(ctx.Mount ?? string.Empty, path, resolvedVersion, data);
            }
            else
            {
                using var doc = await _apiClient.SendAuthenticatedAsync(
                    ctx, HttpMethod.Get, $"v1/{ctx.Mount}/{path}", cancellationToken: cancellationToken).ConfigureAwait(false);

                var envelope = doc.RootElement.Deserialize<VaultKvV1ReadEnvelope>();
                var data = envelope?.Data ?? new Dictionary<string, string>();
                return HashiCorpSecretsReadResult.Ok(ctx.Mount ?? string.Empty, path, version: 0, data);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (HashiCorpApiException ex)
        {
            _logger.LogWarning(ex, "[HashiCorpSecretService] Read failed for {Mount}/{Path}", ctx.Mount, path);
            return HashiCorpSecretsReadResult.Fail(ctx.Mount ?? string.Empty, path, MapErrorCode(ex), ex.Message);
        }
    }

    /// <summary>SEC02 — Write Secret. Supports Check-And-Set (<paramref name="cas"/>) on v2 to prevent lost updates.</summary>
    public async Task<HashiCorpSecretsWriteResult> WriteAsync(
        HashiCorpCallContext ctx, string path, IReadOnlyDictionary<string, string> data, int? cas, CancellationToken cancellationToken = default)
    {
        try
        {
            if (IsV2(ctx))
            {
                object body = cas.HasValue
                    ? new { data, options = new { cas = cas.Value } }
                    : new { data };

                using var doc = await _apiClient.SendAuthenticatedAsync(
                    ctx, HttpMethod.Post, $"v1/{ctx.Mount}/data/{path}", jsonBody: body, cancellationToken: cancellationToken).ConfigureAwait(false);

                var envelope = doc.RootElement.Deserialize<VaultKvV2WriteEnvelope>();
                var version = envelope?.Data?.Version ?? 0;
                return HashiCorpSecretsWriteResult.Ok(ctx.Mount ?? string.Empty, path, version);
            }
            else
            {
                using var _ = await _apiClient.SendAuthenticatedAsync(
                    ctx, HttpMethod.Post, $"v1/{ctx.Mount}/{path}", jsonBody: data, cancellationToken: cancellationToken).ConfigureAwait(false);
                return HashiCorpSecretsWriteResult.Ok(ctx.Mount ?? string.Empty, path, version: 0);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (HashiCorpApiException ex)
        {
            _logger.LogWarning(ex, "[HashiCorpSecretService] Write failed for {Mount}/{Path}", ctx.Mount, path);
            return HashiCorpSecretsWriteResult.Fail(ctx.Mount ?? string.Empty, path, MapErrorCode(ex), ex.Message);
        }
    }

    /// <summary>SEC03 — Delete Secret. Soft delete of latest/specified versions on v2 (permanent on v1 — v1 has no soft delete).</summary>
    public async Task<HashiCorpSecretsDeleteResult> DeleteAsync(
        HashiCorpCallContext ctx, string path, IReadOnlyList<int>? versions, CancellationToken cancellationToken = default)
    {
        try
        {
            if (IsV2(ctx) && versions is { Count: > 0 })
            {
                using var _ = await _apiClient.SendAuthenticatedAsync(
                    ctx, HttpMethod.Post, $"v1/{ctx.Mount}/delete/{path}", jsonBody: new { versions }, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return HashiCorpSecretsDeleteResult.Ok(ctx.Mount ?? string.Empty, path, versions);
            }

            var endpoint = IsV2(ctx) ? $"v1/{ctx.Mount}/data/{path}" : $"v1/{ctx.Mount}/{path}";
            using var __ = await _apiClient.SendAuthenticatedAsync(ctx, HttpMethod.Delete, endpoint, cancellationToken: cancellationToken).ConfigureAwait(false);
            return HashiCorpSecretsDeleteResult.Ok(ctx.Mount ?? string.Empty, path, versions ?? []);
        }
        catch (OperationCanceledException) { throw; }
        catch (HashiCorpApiException ex)
        {
            _logger.LogWarning(ex, "[HashiCorpSecretService] Delete failed for {Mount}/{Path}", ctx.Mount, path);
            return HashiCorpSecretsDeleteResult.Fail(ctx.Mount ?? string.Empty, path, MapErrorCode(ex), ex.Message);
        }
    }

    /// <summary>SEC04 — Undelete Secret Versions (KV v2 only). Restores soft-deleted versions.</summary>
    public async Task<HashiCorpSecretsUndeleteResult> UndeleteAsync(
        HashiCorpCallContext ctx, string path, IReadOnlyList<int> versions, CancellationToken cancellationToken = default)
    {
        try
        {
            using var _ = await _apiClient.SendAuthenticatedAsync(
                ctx, HttpMethod.Post, $"v1/{ctx.Mount}/undelete/{path}", jsonBody: new { versions }, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return HashiCorpSecretsUndeleteResult.Ok(ctx.Mount ?? string.Empty, path, versions);
        }
        catch (OperationCanceledException) { throw; }
        catch (HashiCorpApiException ex)
        {
            _logger.LogWarning(ex, "[HashiCorpSecretService] Undelete failed for {Mount}/{Path}", ctx.Mount, path);
            return HashiCorpSecretsUndeleteResult.Fail(ctx.Mount ?? string.Empty, path, MapErrorCode(ex), ex.Message);
        }
    }

    /// <summary>SEC05 — Destroy Secret Versions (KV v2 only). Permanent — no undo, unlike SEC03.</summary>
    public async Task<HashiCorpSecretsDestroyResult> DestroyAsync(
        HashiCorpCallContext ctx, string path, IReadOnlyList<int> versions, CancellationToken cancellationToken = default)
    {
        try
        {
            using var _ = await _apiClient.SendAuthenticatedAsync(
                ctx, HttpMethod.Post, $"v1/{ctx.Mount}/destroy/{path}", jsonBody: new { versions }, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return HashiCorpSecretsDestroyResult.Ok(ctx.Mount ?? string.Empty, path, versions);
        }
        catch (OperationCanceledException) { throw; }
        catch (HashiCorpApiException ex)
        {
            _logger.LogWarning(ex, "[HashiCorpSecretService] Destroy failed for {Mount}/{Path}", ctx.Mount, path);
            return HashiCorpSecretsDestroyResult.Fail(ctx.Mount ?? string.Empty, path, MapErrorCode(ex), ex.Message);
        }
    }

    /// <summary>
    /// SEC06 — List Secrets. Vault returns 404 for a genuinely empty path — <see cref="HashiCorpApiClient.SendAuthenticatedListAsync"/>
    /// already normalizes that into <c>IsEmpty</c> rather than an exception, so this never maps an empty folder to an error.
    /// </summary>
    public async Task<HashiCorpSecretsListResult> ListAsync(
        HashiCorpCallContext ctx, string path, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = IsV2(ctx) ? $"v1/{ctx.Mount}/metadata/{path}" : $"v1/{ctx.Mount}/{path}";
            var (isEmpty, doc) = await _apiClient.SendAuthenticatedListAsync(ctx, endpoint, cancellationToken).ConfigureAwait(false);

            if (isEmpty || doc is null)
                return HashiCorpSecretsListResult.Ok(ctx.Mount ?? string.Empty, path, []);

            using (doc)
            {
                var envelope = doc.RootElement.Deserialize<VaultListEnvelope>();
                var keys = envelope?.Data?.Keys ?? [];
                return HashiCorpSecretsListResult.Ok(ctx.Mount ?? string.Empty, path, keys);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (HashiCorpApiException ex)
        {
            _logger.LogWarning(ex, "[HashiCorpSecretService] List failed for {Mount}/{Path}", ctx.Mount, path);
            return HashiCorpSecretsListResult.Fail(ctx.Mount ?? string.Empty, path, MapErrorCode(ex), ex.Message);
        }
    }

    /// <summary>SEC07 — Read Secret Metadata (KV v2 only). Version history, cas_required, max_versions, delete_version_after.</summary>
    public async Task<HashiCorpSecretsReadMetadataResult> ReadMetadataAsync(
        HashiCorpCallContext ctx, string path, CancellationToken cancellationToken = default)
    {
        try
        {
            using var doc = await _apiClient.SendAuthenticatedAsync(
                ctx, HttpMethod.Get, $"v1/{ctx.Mount}/metadata/{path}", cancellationToken: cancellationToken).ConfigureAwait(false);

            var envelope = doc.RootElement.Deserialize<VaultMetadataReadEnvelope>();
            var d = envelope?.Data;
            if (d is null)
                return HashiCorpSecretsReadMetadataResult.Fail(ctx.Mount ?? string.Empty, path, "HASHICORP_UPSTREAM_ERROR", "Vault returned no metadata body.");

            var versions = (d.Versions ?? new Dictionary<string, VaultMetadataVersionEntry>())
                .Select(kv => new HashiCorpSecretVersionInfo(
                    int.TryParse(kv.Key, out var v) ? v : 0,
                    kv.Value.CreatedTime,
                    string.IsNullOrEmpty(kv.Value.DeletionTime) ? null : DateTimeOffset.Parse(kv.Value.DeletionTime),
                    kv.Value.Destroyed))
                .OrderBy(v => v.Version)
                .ToList();

            var metadata = new HashiCorpSecretMetadata(path, d.CurrentVersion, d.OldestVersion, d.MaxVersions, d.CasRequired, versions);
            return HashiCorpSecretsReadMetadataResult.Ok(ctx.Mount ?? string.Empty, path, metadata);
        }
        catch (OperationCanceledException) { throw; }
        catch (HashiCorpApiException ex)
        {
            _logger.LogWarning(ex, "[HashiCorpSecretService] ReadMetadata failed for {Mount}/{Path}", ctx.Mount, path);
            return HashiCorpSecretsReadMetadataResult.Fail(ctx.Mount ?? string.Empty, path, MapErrorCode(ex), ex.Message);
        }
    }

    /// <summary>SEC08 — Update Secret Metadata (KV v2 only). Configures retention without writing new data.</summary>
    public async Task<HashiCorpSecretsUpdateMetadataResult> UpdateMetadataAsync(
        HashiCorpCallContext ctx, string path, int? maxVersions, bool? casRequired, CancellationToken cancellationToken = default)
    {
        try
        {
            var body = new Dictionary<string, object>();
            if (maxVersions.HasValue) body["max_versions"] = maxVersions.Value;
            if (casRequired.HasValue) body["cas_required"] = casRequired.Value;

            using var _ = await _apiClient.SendAuthenticatedAsync(
                ctx, HttpMethod.Post, $"v1/{ctx.Mount}/metadata/{path}", jsonBody: body, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return HashiCorpSecretsUpdateMetadataResult.Ok(ctx.Mount ?? string.Empty, path, maxVersions, casRequired);
        }
        catch (OperationCanceledException) { throw; }
        catch (HashiCorpApiException ex)
        {
            _logger.LogWarning(ex, "[HashiCorpSecretService] UpdateMetadata failed for {Mount}/{Path}", ctx.Mount, path);
            return HashiCorpSecretsUpdateMetadataResult.Fail(ctx.Mount ?? string.Empty, path, MapErrorCode(ex), ex.Message);
        }
    }

    // ── Shared error mapping ─────────────────────────────────────────────────

    internal static string MapErrorCode(HashiCorpApiException ex) => ex.StatusCode switch
    {
        0 => "HASHICORP_VAULT_UNREACHABLE", // sentinel: no HTTP response at all — timeout/DNS/connection failure
        503 => "HASHICORP_SEALED",
        403 => "HASHICORP_PERMISSION_DENIED",
        404 => "HASHICORP_SECRET_NOT_FOUND",
        400 when ex.VaultErrors.Any(e => e.Contains("check-and-set", StringComparison.OrdinalIgnoreCase) ||
                                          e.Contains("cas", StringComparison.OrdinalIgnoreCase))
            => "HASHICORP_CAS_MISMATCH",
        >= 500 and < 600 => "HASHICORP_UPSTREAM_ERROR",
        _ => "HASHICORP_UPSTREAM_ERROR",
    };
}
