namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

/// <summary>
/// Common per-operation config every HashiCorp Vault operation shares: which Vault server to talk
/// to, how to authenticate, and (for Secrets operations only) which KV mount/engine version.
/// <see cref="Mount"/>/<see cref="EngineVersion"/> live here rather than on a Secrets-only base so
/// that <c>HashiCorpNodeExecutor.Credentials.cs</c> has one call-context-building path for every
/// operation instead of a per-resource variant — Token/Lease/System operations simply leave them null.
/// </summary>
public abstract class BaseHashiCorpOperationInfo : BaseNodeOperationInfo
{
    /// <summary>The tenant's Vault server base URL, e.g. <c>https://vault.internal.example.com:8200</c>. No BizFirst-wide default exists.</summary>
    public string? VaultAddress { get; private set; }

    /// <summary><c>"token"</c> or <c>"appRole"</c>. Required except for <c>system/health</c> and <c>system/sealStatus</c>, which are unauthenticated.</summary>
    public string? AuthMethod { get; private set; }

    /// <summary>BizFirst credential record ID, resolved via <c>IHashiCorpCredentialResolver</c>. Required except for the unauthenticated System operations.</summary>
    public int? CredentialID { get; private set; }

    /// <summary>Vault auth-method mount path, default <c>"approle"</c>. Only meaningful when <see cref="AuthMethod"/> is <c>"appRole"</c>.</summary>
    public string? AppRolePath { get; private set; }

    /// <summary>Optional Vault Enterprise namespace, sent as <c>X-Vault-Namespace</c> if present.</summary>
    public string? Namespace { get; private set; }

    /// <summary>KV engine mount path, e.g. <c>"secret"</c>. Only set by Secrets operations.</summary>
    public string? Mount { get; private set; }

    /// <summary><c>"1"</c> or <c>"2"</c>, default <c>"2"</c>. Only set by Secrets operations.</summary>
    public string? EngineVersion { get; private set; }

    public override void LoadFrom(ConfigDataPropertyBag reader)
    {
        VaultAddress  = reader.ReadConfigByKeyDefaultNull("vaultAddress");
        AuthMethod    = reader.ReadConfigByKeyDefaultNull("authMethod");
        CredentialID  = reader.ReadConfigByKey_Int("credentialID");
        AppRolePath   = reader.ReadConfigByKey("appRolePath", "approle");
        Namespace     = reader.ReadConfigByKeyDefaultNull("namespace");

        if (RequiresMount)
        {
            Mount         = reader.ReadConfigByKeyDefaultNull("mount");
            EngineVersion = reader.ReadConfigByKey("engineVersion", "2");
        }
    }

    /// <summary>True for every Secrets operation (SEC01–SEC08); false for Token/Lease/System, which are not KV-scoped.</summary>
    protected virtual bool RequiresMount => false;

    /// <summary>True only for the two unauthenticated System operations (health/seal-status) — skips credential resolution entirely.</summary>
    public virtual bool IsUnauthenticated => false;

    /// <summary>
    /// Vault Enterprise namespaces are slash-separated hierarchical paths (e.g. <c>parent/child</c>).
    /// Rejects leading/trailing slashes, empty segments, and characters outside the set Vault itself
    /// accepts in a namespace path, so a typo'd namespace fails fast at config-validation time instead
    /// of silently routing to the wrong namespace or surfacing as an opaque Vault 400/404.
    /// </summary>
    private static readonly System.Text.RegularExpressions.Regex NamespacePattern =
        new(@"^[A-Za-z0-9_-]+(/[A-Za-z0-9_-]+)*$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public (string Code, string Message)? Validate()
    {
        if (string.IsNullOrWhiteSpace(VaultAddress))
            return ("HASHICORP_INVALID_CONFIGURATION", "Config key 'vaultAddress' is required.");

        if (!string.IsNullOrEmpty(Namespace) && !NamespacePattern.IsMatch(Namespace))
            return ("HASHICORP_INVALID_CONFIGURATION", "Config key 'namespace' must not have leading/trailing slashes or empty segments, e.g. 'parent/child'.");

        if (!IsUnauthenticated)
        {
            if (string.IsNullOrWhiteSpace(AuthMethod) || (AuthMethod != "token" && AuthMethod != "appRole"))
                return ("HASHICORP_INVALID_CONFIGURATION", "Config key 'authMethod' must be 'token' or 'appRole'.");

            if (!CredentialID.HasValue)
                return ("HASHICORP_INVALID_CONFIGURATION", "Config key 'credentialID' is required.");
        }

        if (RequiresMount && string.IsNullOrWhiteSpace(Mount))
            return ("HASHICORP_INVALID_CONFIGURATION", "Config key 'mount' is required.");

        return ValidateOperation();
    }

    /// <summary>Override to add operation-specific validation beyond the universal keys above.</summary>
    protected virtual (string Code, string Message)? ValidateOperation() => null;

    public override Dictionary<string, object> ToDictionary()
    {
        var d = new Dictionary<string, object>
        {
            ["vaultAddress"] = VaultAddress ?? string.Empty,
            ["authMethod"]   = AuthMethod ?? string.Empty,
        };
        if (CredentialID.HasValue) d["credentialID"] = CredentialID.Value;
        if (!string.IsNullOrWhiteSpace(AppRolePath)) d["appRolePath"] = AppRolePath;
        if (!string.IsNullOrWhiteSpace(Namespace)) d["namespace"] = Namespace;
        if (RequiresMount)
        {
            d["mount"] = Mount ?? string.Empty;
            d["engineVersion"] = EngineVersion ?? "2";
        }
        AddOperationFields(d);
        return d;
    }

    /// <summary>Override to add operation-specific fields to the serialized settings dictionary.</summary>
    protected virtual void AddOperationFields(Dictionary<string, object> d)
    {
    }

    // ── Shared parsing helpers for operation-specific keys ───────────────────

    /// <summary>Reads a config key expected to hold a JSON array of integers (e.g. <c>versions</c>).</summary>
    protected static IReadOnlyList<int> ReadIntArray(ConfigDataPropertyBag reader, string key)
    {
        var node = reader.ReadConfigNodeByKey(key);
        if (node is not System.Text.Json.Nodes.JsonArray array)
            return [];

        var result = new List<int>(array.Count);
        foreach (var item in array)
        {
            if (item is not null && int.TryParse(item.ToString(), out var value))
                result.Add(value);
        }
        return result;
    }

    /// <summary>Reads a config key expected to hold a flat string/string map (e.g. <c>secretData</c>).</summary>
    protected static IReadOnlyDictionary<string, string> ReadStringDictionary(ConfigDataPropertyBag reader, string key)
    {
        var raw = reader.ReadConfigDictionaryByKey(key);
        var result = new Dictionary<string, string>(raw.Count);
        foreach (var kv in raw)
            result[kv.Key] = kv.Value?.ToString() ?? string.Empty;
        return result;
    }
}
