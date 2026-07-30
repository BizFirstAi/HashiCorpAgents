using BizFirst.Integration.HashiCorp.Domain;

namespace BizFirst.Integration.HashiCorp.Services.Http;

/// <summary>
/// The single place that knows how to talk to <em>a</em> Vault server. Handed the tenant's
/// <c>vaultAddress</c> per call via <see cref="HashiCorpCallContext"/> — never hardcodes a host,
/// unlike API clients for hosted SaaS integrations. Owns retry/backoff and low-level error mapping;
/// resource services (<see cref="HashiCorpSecretService"/> etc.) never touch <see cref="HttpClient"/>
/// directly.
///
/// Retry policy (see the design review's §4.4):
///   503 (sealed)              → never retried, thrown immediately.
///   429 (rate limit/standby)  → retried with exponential backoff, honoring Retry-After if present.
///   other 5xx                 → retried with exponential backoff.
///   other 4xx                 → never retried, thrown immediately.
///   OperationCanceledException → never caught/swallowed, always the first catch.
/// </summary>
public sealed class HashiCorpApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ILogger<HashiCorpApiClient> _logger;
    private readonly HashiCorpApiClientOptions _options;

    public HashiCorpApiClient(HttpClient http, ILogger<HashiCorpApiClient> logger, HashiCorpApiClientOptions? options = null)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new HashiCorpApiClientOptions();
    }

    /// <summary>Authenticated GET/POST/DELETE. Throws <see cref="HashiCorpApiException"/> on any non-2xx after retries.</summary>
    public async Task<JsonDocument> SendAuthenticatedAsync(
        HashiCorpCallContext ctx,
        HttpMethod method,
        string relativePath,
        IReadOnlyDictionary<string, string>? query = null,
        object? jsonBody = null,
        CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        var delay = _options.InitialRetryDelay;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var request = BuildRequest(ctx, method, relativePath, query, jsonBody);

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // Deliberately NOT rethrown as-is: TaskCanceledException IS-A OperationCanceledException,
                // so a bare `throw;` here would later be swallowed-and-rethrown by every upstream
                // `catch (OperationCanceledException) { throw; }` (the mandatory pattern per §4.4) as if
                // this were a real workflow cancellation, when it is actually an HttpClient timeout —
                // misreporting a Vault connectivity problem as the caller's own cancellation. Wrapping as
                // HashiCorpApiException(0, ...) instead lets it flow through those guards intact and be
                // caught by the same `catch (HashiCorpApiException)` every service method already has,
                // mapping to HASHICORP_VAULT_UNREACHABLE via each service's MapErrorCode.
                _logger.LogError(ex, "[HashiCorpApiClient] Request timed out: {Method} {Path}", method, relativePath);
                throw new HashiCorpApiException(0, [$"Vault request timed out: {method} {relativePath}"]);
            }
            catch (OperationCanceledException) { throw; }
            catch (HttpRequestException ex)
            {
                // DNS failure, connection refused, TLS failure, etc. — Vault never responded at all.
                // Status code 0 is HashiCorpApiException's sentinel for "no HTTP response," distinct
                // from any real Vault status code — see MapErrorCode in each resource service.
                _logger.LogError(ex, "[HashiCorpApiClient] Network failure calling {Method} {Path}", method, relativePath);
                throw new HashiCorpApiException(0, [ex.Message]);
            }

            using (response)
            {
                var statusCode = (int)response.StatusCode;

                if (statusCode is >= 200 and < 300)
                    return await ParseBodyAsync(response, cancellationToken).ConfigureAwait(false);

                var errors = await ReadVaultErrorsAsync(response, cancellationToken).ConfigureAwait(false);

                // 503 = sealed. Never retry — a human needs to unseal Vault, looping wastes time.
                if (statusCode == 503)
                    throw new HashiCorpApiException(statusCode, errors);

                var retryable = statusCode == 429 || statusCode is >= 500 and < 600;
                if (retryable && attempt < _options.MaxRetries)
                {
                    var wait = response.Headers.RetryAfter?.Delta ?? delay;
                    _logger.LogWarning(
                        "[HashiCorpApiClient] {StatusCode} on {Method} {Path}, retrying in {Wait} (attempt {Attempt}/{Max})",
                        statusCode, method, relativePath, wait, attempt + 1, _options.MaxRetries);
                    await Task.Delay(wait, cancellationToken).ConfigureAwait(false);
                    attempt++;
                    delay += delay; // exponential backoff
                    continue;
                }

                throw new HashiCorpApiException(statusCode, errors);
            }
        }
    }

    /// <summary>
    /// Authenticated LIST (GET with <c>?list=true</c>). Vault returns 404 with an empty <c>errors</c>
    /// array both when the path is missing and when it exists with no children — both cases are
    /// reported back as <c>(IsEmpty: true, Document: null)</c>, never as a thrown exception, so
    /// callers can render "no secrets here" instead of an error.
    /// </summary>
    public async Task<(bool IsEmpty, JsonDocument? Document)> SendAuthenticatedListAsync(
        HashiCorpCallContext ctx,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        using var request = BuildRequest(ctx, HttpMethod.Get, relativePath, new Dictionary<string, string> { ["list"] = "true" }, jsonBody: null);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // See SendAuthenticatedAsync's identical catch for why this wraps rather than rethrows.
            _logger.LogError(ex, "[HashiCorpApiClient] LIST request timed out: {Path}", relativePath);
            throw new HashiCorpApiException(0, [$"Vault LIST request timed out: {relativePath}"]);
        }
        catch (OperationCanceledException) { throw; }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[HashiCorpApiClient] Network failure calling LIST {Path}", relativePath);
            throw new HashiCorpApiException(0, [ex.Message]);
        }

        using (response)
        {
            var statusCode = (int)response.StatusCode;
            if (statusCode is >= 200 and < 300)
                return (false, await ParseBodyAsync(response, cancellationToken).ConfigureAwait(false));

            var errors = await ReadVaultErrorsAsync(response, cancellationToken).ConfigureAwait(false);
            if (statusCode == 404 && errors.Count == 0)
                return (true, null);

            throw new HashiCorpApiException(statusCode, errors);
        }
    }

    /// <summary>
    /// Unauthenticated call for <c>sys/health</c>/<c>sys/seal-status</c>. Vault's health endpoint
    /// deliberately answers with non-2xx codes to encode cluster state (429 standby, 503 sealed,
    /// etc.) — those are meaningful states, not failures, so this never throws on the HTTP status;
    /// only a genuine network failure surfaces as an exception.
    /// </summary>
    public async Task<(int StatusCode, JsonDocument Document)> SendUnauthenticatedRawAsync(
        string vaultAddress,
        string relativePath,
        IReadOnlyDictionary<string, string>? query = null,
        CancellationToken cancellationToken = default)
    {
        var ctx = new HashiCorpCallContext(vaultAddress, ClientToken: null, Mount: null, EngineVersion: null, Namespace: null);
        using var request = BuildRequest(ctx, HttpMethod.Get, relativePath, query, jsonBody: null);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // See SendAuthenticatedAsync's identical catch — same HashiCorpApiException(0, ...) wrapping,
            // so callers only ever need one catch type regardless of which Send*Async method they used.
            _logger.LogError(ex, "[HashiCorpApiClient] Unauthenticated request timed out: {Path}", relativePath);
            throw new HashiCorpApiException(0, [$"Vault request timed out: {relativePath}"]);
        }
        catch (OperationCanceledException) { throw; }
        catch (HttpRequestException ex)
        {
            // DNS failure, connection refused, TLS failure, etc. — see SendAuthenticatedAsync's identical catch.
            _logger.LogError(ex, "[HashiCorpApiClient] Network failure calling unauthenticated {Path}", relativePath);
            throw new HashiCorpApiException(0, [ex.Message]);
        }

        using (response)
        {
            var doc = await ParseBodyAsync(response, cancellationToken).ConfigureAwait(false);
            return ((int)response.StatusCode, doc);
        }
    }

    // ── Request building ─────────────────────────────────────────────────────

    private static HttpRequestMessage BuildRequest(
        HashiCorpCallContext ctx,
        HttpMethod method,
        string relativePath,
        IReadOnlyDictionary<string, string>? query,
        object? jsonBody)
    {
        var uri = BuildUri(ctx.VaultAddress, relativePath, query);
        var request = new HttpRequestMessage(method, uri);

        if (!string.IsNullOrWhiteSpace(ctx.ClientToken))
            request.Headers.TryAddWithoutValidation("X-Vault-Token", ctx.ClientToken);

        if (!string.IsNullOrWhiteSpace(ctx.Namespace))
            request.Headers.TryAddWithoutValidation("X-Vault-Namespace", ctx.Namespace);

        if (jsonBody is not null)
            request.Content = JsonContent.Create(jsonBody, options: SerializerOptions);

        return request;
    }

    private static Uri BuildUri(string vaultAddress, string relativePath, IReadOnlyDictionary<string, string>? query)
    {
        var baseUri = vaultAddress.TrimEnd('/');

        // relativePath is built by callers via string interpolation of user-supplied values
        // (Mount, secret Path — which itself is a legitimately "/"-separated hierarchy, e.g.
        // "myapp/db/prod") directly into a route template. Escaping the whole string at once
        // would corrupt the "/" separators; escaping per-segment preserves them as delimiters
        // while safely handling spaces, '#', '?', '%', etc. within any single segment — without
        // this, a secret path containing any of those breaks the request or silently targets the
        // wrong path, since they'd otherwise be interpreted as URL syntax instead of literal data.
        var path = string.Join('/', relativePath
            .TrimStart('/')
            .Split('/')
            .Select(segment => Uri.EscapeDataString(segment)));

        var url = $"{baseUri}/{path}";

        if (query is { Count: > 0 })
        {
            var pairs = query.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}");
            url += "?" + string.Join("&", pairs);
        }

        return new Uri(url, UriKind.Absolute);
    }

    // ── Response parsing ──────────────────────────────────────────────────────

    private static async Task<JsonDocument> ParseBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        if (stream.Length == 0)
            return JsonDocument.Parse("{}");

        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<string>> ReadVaultErrorsAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            using var doc = await ParseBodyAsync(response, cancellationToken).ConfigureAwait(false);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("errors", out var errorsElement) &&
                errorsElement.ValueKind == JsonValueKind.Array)
            {
                return errorsElement.EnumerateArray()
                    .Select(e => e.GetString() ?? string.Empty)
                    .Where(s => s.Length > 0)
                    .ToList();
            }
        }
        catch (JsonException)
        {
            // Non-JSON error body — fall through to an empty error list; the status code alone still routes correctly.
        }

        return [];
    }
}
