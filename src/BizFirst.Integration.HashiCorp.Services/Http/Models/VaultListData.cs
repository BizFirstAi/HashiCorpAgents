namespace BizFirst.Integration.HashiCorp.Services.Http.Models;

internal sealed class VaultListData
{
    [JsonPropertyName("keys")] public List<string>? Keys { get; set; }
}
