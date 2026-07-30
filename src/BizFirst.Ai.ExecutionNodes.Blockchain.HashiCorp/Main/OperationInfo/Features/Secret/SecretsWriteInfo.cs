namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

/// <summary>SEC02 — secrets/write. Writes a flat key/value map; <see cref="Cas"/> enables Check-And-Set on v2 to prevent lost updates.</summary>
internal sealed class SecretsWriteInfo : BaseHashiCorpOperationInfo
{
    public string? Path { get; private set; }
    public IReadOnlyDictionary<string, string> Data { get; private set; } = new Dictionary<string, string>();
    public int? Cas { get; private set; }

    protected override bool RequiresMount => true;

    public override void LoadFrom(ConfigDataPropertyBag reader)
    {
        base.LoadFrom(reader);
        Path = reader.ReadConfigByKeyDefaultNull("path");
        Data = ReadStringDictionary(reader, "data");
        Cas = reader.ReadConfigByKey_Int("cas");
    }

    protected override (string Code, string Message)? ValidateOperation()
    {
        if (string.IsNullOrWhiteSpace(Path))
            return ("HASHICORP_INVALID_CONFIGURATION", "Config key 'path' is required for secrets/write.");
        if (Data.Count == 0)
            return ("HASHICORP_INVALID_CONFIGURATION", "Config key 'data' must contain at least one field for secrets/write.");
        return null;
    }

    protected override void AddOperationFields(Dictionary<string, object> d)
    {
        d["path"] = Path ?? string.Empty;
        if (Cas.HasValue) d["cas"] = Cas.Value;
    }
}
