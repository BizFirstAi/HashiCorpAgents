namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

/// <summary>Typed settings for <see cref="HashiCorpNodeExecutor"/> — resolves the active operation DTO from (resource, operation).</summary>
internal sealed class HashiCorpNodeExecutorSettings : ResourceBasedNodeExecutorSettings
{
    public override BaseNodeOperationInfo? CreateActiveInfo() =>
        HashiCorpOperationInfoFactory.Create(Resource, Operation, ConfigReader);
}
