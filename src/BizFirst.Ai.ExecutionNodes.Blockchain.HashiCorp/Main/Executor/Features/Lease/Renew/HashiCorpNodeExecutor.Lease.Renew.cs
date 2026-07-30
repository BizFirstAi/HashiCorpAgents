// <summary>
// Code review guidelines: 020_NodeServerProject-Engineer/Guidelines/14_node-executor-integration-code/guideline.md
// </summary>
using BizFirst.Ai.ProcessEngine.Service;
namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

//IMPORTANT: "code-step" comments must not be changed. This is a coding checklist used as a template.
public sealed partial class HashiCorpNodeExecutor
{
    private async Task<NodeExecutionResult> _HashiCorp_Lease_RenewAsync(
        NodeExecutionContext nodeExecutionContext,
        CancellationToken cancellationToken = default)
    {
        //code-step: 1.1 - Validate settings exist and cast to LeaseRenewInfo
        if (mySettings?.ActiveInfo is not LeaseRenewInfo info)
            return SimpleErrorOperationUnfound();

        var validationError = info.Validate();
        if (validationError is not null)
            return GetBuildErrorOutput_NodeExecutionResult_WithWarningLogger(validationError.Value.Code, validationError.Value.Message);

        var (ctx, credentialError) = await _ResolveCallContextAsync(info, cancellationToken);
        if (credentialError is not null)
            return credentialError;

        try
        {
            //code-step: 1.3 - Call HashiCorp lease service to extend a dynamic secret's lease
            var r = await _leaseService.RenewAsync(ctx!, info.LeaseID!, info.IncrementSeconds, cancellationToken);

            if (!r.Success)
                return GetBuildErrorOutput_NodeExecutionResult_WithWarningLogger(r.ErrorCode, r.ErrorMessage);

            //code-step: 1.4 - Report progress milestone to execution context
            await ReportNodeProgress_ResourceOperation(nodeExecutionContext, "IntegrationCallCompleted");

            //code-step: 1.5 - Extract renewed lease from result
            var renewed = new Dictionary<string, object>
            {
                ["leaseID"]   = r.LeaseID,
                ["ttl"]       = r.Ttl,
                ["renewable"] = r.Renewable,
            };

            //code-step: 1.6 - Build output metadata dictionary
            var resultManager = NodeResultOperateManager.CreateInstance(nodeExecutionContext);
            var outputData = resultManager.GetOrCreateOutputData();
            outputData["status"]    = "success";
            outputData["resource"]  = mySettings.Resource ?? "lease";
            outputData["operation"] = mySettings.Operation ?? "renew";
            outputData["errorCode"] = string.Empty;
            outputData["leaseID"]   = r.LeaseID;
            outputData["ttl"]       = r.Ttl;
            outputData["renewable"] = r.Renewable;

            //code-step: 1.7 - Convert renewed lease to standard items array
            outputData.TryGetValue(ExecutionConstants.OutputFieldNameConstants.CONST_items, out var existingItemsValue);
            outputData[ExecutionConstants.OutputFieldNameConstants.CONST_items] = ApplyOutputItemsMerge(existingItemsValue, WrapSingleObjectIntoItems(renewed, nodeExecutionContext));

            //code-step: 1.8 - Write output (handles TargetDataPath writes + items downstream)
            return await WriteOutputData(ExecutionConstants.OutputPorts.Success, outputData, renewed, nodeExecutionContext, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            //code-step: 1.9 - Catch unexpected exceptions and return error with context
            return GetBuildErrorOutput_NodeExecutionResult_WithLogging("HASHICORP_UPSTREAM_ERROR", $"lease/renew failed for {info.LeaseID}: {ex.Message}", ex);
        }
    }
}
