// <summary>
// Code review guidelines: 020_NodeServerProject-Engineer/Guidelines/14_node-executor-integration-code/guideline.md
// </summary>
using BizFirst.Ai.ProcessEngine.Service;
namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

//IMPORTANT: "code-step" comments must not be changed. This is a coding checklist used as a template.
public sealed partial class HashiCorpNodeExecutor
{
    private async Task<NodeExecutionResult> _HashiCorp_Secrets_ReadAsync(
        NodeExecutionContext nodeExecutionContext,
        CancellationToken cancellationToken = default)
    {
        //code-step: 1.1 - Validate settings exist and cast to SecretsReadInfo
        if (mySettings?.ActiveInfo is not SecretsReadInfo info)
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
            //code-step: 1.3 - Call HashiCorp secret service to read the secret
            var r = await _secretService.ReadAsync(ctx!, info.Path!, info.Version, cancellationToken);

            if (!r.Success)
                return GetBuildErrorOutput_NodeExecutionResult_WithWarningLogger(r.ErrorCode, r.ErrorMessage);

            //code-step: 1.4 - Report progress milestone to execution context
            await ReportNodeProgress_ResourceOperation(nodeExecutionContext, "IntegrationCallCompleted");

            //code-step: 1.5 - Extract secret data record from result
            var record = new Dictionary<string, object>();
            foreach (var kv in r.Data)
                record[kv.Key] = kv.Value;
            record["mount"] = r.Mount;
            record["path"] = r.Path;
            record["version"] = r.Version;

            //code-step: 1.6 - Build output metadata dictionary
            var outputData = resultManager.GetOrCreateOutputData();
            outputData["status"] = "success";
            outputData["resource"] = mySettings.Resource ?? "secrets";
            outputData["operation"] = mySettings.Operation ?? "read";

            //code-step: 1.7 - Convert secret data to standard items array
            outputData.TryGetValue(ExecutionConstants.OutputFieldNameConstants.CONST_items, out var existingItemsValue);
            outputData[ExecutionConstants.OutputFieldNameConstants.CONST_items] = ApplyOutputItemsMerge(existingItemsValue, WrapSingleObjectIntoItems(record, nodeExecutionContext));

            //code-step: 1.8 - Write output (handles TargetDataPath writes + items downstream)
            return await WriteOutputData(ExecutionConstants.OutputPorts.Success, outputData, record, nodeExecutionContext, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            //code-step: 1.9 - Catch unexpected exceptions and return error with context
            return GetBuildErrorOutput_NodeExecutionResult_WithLogging("HASHICORP_UPSTREAM_ERROR", $"secrets/read failed: {ex.Message}", ex);
        }
    }
}
