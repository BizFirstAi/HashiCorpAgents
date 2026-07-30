// <summary>
// Code review guidelines: 020_NodeServerProject-Engineer/Guidelines/14_node-executor-integration-code/guideline.md
// </summary>
using BizFirst.Ai.ProcessEngine.Service;
namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

//IMPORTANT: "code-step" comments must not be changed. This is a coding checklist used as a template.
public sealed partial class HashiCorpNodeExecutor
{
    private async Task<NodeExecutionResult> _HashiCorp_Secrets_ReadMetadataAsync(
        NodeExecutionContext nodeExecutionContext,
        CancellationToken cancellationToken = default)
    {
        //code-step: 1.1 - Validate settings exist and cast to SecretsReadMetadataInfo
        if (mySettings?.ActiveInfo is not SecretsReadMetadataInfo info)
            return SimpleErrorOperationUnfound();

        var validationError = info.Validate();
        if (validationError is not null)
            return GetBuildErrorOutput_NodeExecutionResult_WithWarningLogger(validationError.Value.Code, validationError.Value.Message);

        var (ctx, credentialError) = await _ResolveCallContextAsync(info, cancellationToken);
        if (credentialError is not null)
            return credentialError;

        //code-step: 1.2 - Create result manager for output handling
        var resultManager = NodeResultOperateManager.CreateInstance(nodeExecutionContext);

        try
        {
            //code-step: 1.3 - Call HashiCorp secret service to read version metadata
            var r = await _secretService.ReadMetadataAsync(ctx!, info.Path!, cancellationToken);

            if (!r.Success || r.Metadata is null)
                return GetBuildErrorOutput_NodeExecutionResult_WithWarningLogger(r.ErrorCode, r.ErrorMessage);

            //code-step: 1.4 - Report progress milestone to execution context
            await ReportNodeProgress_ResourceOperation(nodeExecutionContext, "IntegrationCallCompleted");

            //code-step: 1.5 - Extract metadata record from result
            var metadata = r.Metadata;
            var record = new Dictionary<string, object>
            {
                ["mount"] = r.Mount,
                ["path"] = metadata.Path,
                ["currentVersion"] = metadata.CurrentVersion,
                ["oldestVersion"] = metadata.OldestVersion,
                ["maxVersions"] = metadata.MaxVersions,
                ["casRequired"] = metadata.CasRequired,
                ["versions"] = metadata.Versions.Select(v => new Dictionary<string, object>
                {
                    ["version"] = v.Version,
                    ["createdTime"] = v.CreatedTime,
                    ["deletionTime"] = (object?)v.DeletionTime ?? string.Empty,
                    ["destroyed"] = v.Destroyed,
                }).ToList(),
            };

            //code-step: 1.6 - Build output metadata dictionary
            var outputData = resultManager.GetOrCreateOutputData();
            outputData["status"] = "success";
            outputData["resource"] = mySettings.Resource ?? "secrets";
            outputData["operation"] = mySettings.Operation ?? "readMetadata";

            //code-step: 1.7 - Convert metadata record to standard items array
            outputData.TryGetValue(ExecutionConstants.OutputFieldNameConstants.CONST_items, out var existingItemsValue);
            outputData[ExecutionConstants.OutputFieldNameConstants.CONST_items] = ApplyOutputItemsMerge(existingItemsValue, WrapSingleObjectIntoItems(record, nodeExecutionContext));

            //code-step: 1.8 - Write output (handles TargetDataPath writes + items downstream)
            return await WriteOutputData(ExecutionConstants.OutputPorts.Success, outputData, record, nodeExecutionContext, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            //code-step: 1.9 - Catch unexpected exceptions and return error with context
            return GetBuildErrorOutput_NodeExecutionResult_WithLogging("HASHICORP_UPSTREAM_ERROR", $"secrets/readMetadata failed: {ex.Message}", ex);
        }
    }
}
