namespace BizFirst.Integration.HashiCorp.Services.Http.Models;

/// <summary>List response envelope: <c>{"data": {"keys": [...]}}</c>.</summary>
internal sealed class VaultListEnvelope
{
    [JsonPropertyName("data")] public VaultListData? Data { get; set; }
}
