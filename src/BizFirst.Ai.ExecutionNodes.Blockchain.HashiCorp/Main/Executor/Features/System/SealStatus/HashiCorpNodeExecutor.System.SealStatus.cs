// <summary>
// Code review guidelines: 020_NodeServerProject-Engineer/Guidelines/14_node-executor-integration-code/guideline.md
// </summary>
using BizFirst.Ai.ProcessEngine.Service;
namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

//IMPORTANT: "code-step" comments must not be changed. This is a coding checklist used as a template.
public sealed partial class HashiCorpNodeExecutor
{
    private async Task<NodeExecutionResult> _HashiCorp_System_SealStatusAsync(
        NodeExecutionContext nodeExecutionContext,
        CancellationToken cancellationToken = default)
    {
        //code-step: 1.1 - Validate settings exist and cast to SystemSealStatusInfo
        if (mySettings?.ActiveInfo is not SystemSealStatusInfo info)
            return SimpleErrorOperationUnfound();

        var validationError = info.Validate();
        if (validationError is not null)
            return GetBuildErrorOutput_NodeExecutionResult_WithWarningLogger(validationError.Value.Code, validationError.Value.Message);

        // Unauthenticated — deliberately skips _ResolveCallContextAsync/credential resolution entirely.

        try
        {
            //code-step: 1.3 - Call HashiCorp system service to check seal status (answers even when Vault is sealed)
            var r = await _systemService.SealStatusAsync(info.VaultAddress!, cancellationToken);

            if (!r.Success)
                return GetBuildErrorOutput_NodeExecutionResult_WithWarningLogger(r.ErrorCode, r.ErrorMessage);

            //code-step: 1.4 - Report progress milestone to execution context
            await ReportNodeProgress_ResourceOperation(nodeExecutionContext, "IntegrationCallCompleted");

            //code-step: 1.5 - Extract seal status from result
            var seal = new Dictionary<string, object>
            {
                ["sealed"]   = r.Sealed,
                ["t"]        = r.T,
                ["n"]        = r.N,
                ["progress"] = r.Progress,
            };

            //code-step: 1.6 - Build output metadata dictionary
            var resultManager = NodeResultOperateManager.CreateInstance(nodeExecutionContext);
            var outputData = resultManager.GetOrCreateOutputData();
            outputData["status"]    = "success";
            outputData["resource"]  = mySettings.Resource ?? "system";
            outputData["operation"] = mySettings.Operation ?? "sealStatus";
            outputData["errorCode"] = string.Empty;
            outputData["sealed"]    = r.Sealed;
            outputData["t"]         = r.T;
            outputData["n"]         = r.N;
            outputData["progress"]  = r.Progress;

            //code-step: 1.7 - Convert seal status to standard items array
            outputData.TryGetValue(ExecutionConstants.OutputFieldNameConstants.CONST_items, out var existingItemsValue);
            outputData[ExecutionConstants.OutputFieldNameConstants.CONST_items] = ApplyOutputItemsMerge(existingItemsValue, WrapSingleObjectIntoItems(seal, nodeExecutionContext));

            //code-step: 1.8 - Write output (handles TargetDataPath writes + items downstream)
            return await WriteOutputData(ExecutionConstants.OutputPorts.Success, outputData, seal, nodeExecutionContext, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            //code-step: 1.9 - Catch unexpected exceptions and return error with context
            return GetBuildErrorOutput_NodeExecutionResult_WithLogging("HASHICORP_VAULT_UNREACHABLE", $"system/sealStatus failed for {info.VaultAddress}: {ex.Message}", ex);
        }
    }
}
