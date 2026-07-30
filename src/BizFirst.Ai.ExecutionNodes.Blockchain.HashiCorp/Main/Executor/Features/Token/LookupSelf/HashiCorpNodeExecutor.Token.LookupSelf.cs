// <summary>
// Code review guidelines: 020_NodeServerProject-Engineer/Guidelines/14_node-executor-integration-code/guideline.md
// </summary>
using BizFirst.Ai.ProcessEngine.Service;
namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

//IMPORTANT: "code-step" comments must not be changed. This is a coding checklist used as a template.
public sealed partial class HashiCorpNodeExecutor
{
    private async Task<NodeExecutionResult> _HashiCorp_Token_LookupSelfAsync(
        NodeExecutionContext nodeExecutionContext,
        CancellationToken cancellationToken = default)
    {
        //code-step: 1.1 - Validate settings exist and cast to TokenLookupSelfInfo
        if (mySettings?.ActiveInfo is not TokenLookupSelfInfo info)
            return SimpleErrorOperationUnfound();

        var validationError = info.Validate();
        if (validationError is not null)
            return GetBuildErrorOutput_NodeExecutionResult_WithWarningLogger(validationError.Value.Code, validationError.Value.Message);

        var (ctx, credentialError) = await _ResolveCallContextAsync(info, cancellationToken);
        if (credentialError is not null)
            return credentialError;

        try
        {
            //code-step: 1.3 - Call HashiCorp token service to look up this token's own TTL/policies
            var r = await _tokenService.LookupSelfAsync(ctx!, cancellationToken);

            if (!r.Success || r.Token is null)
                return GetBuildErrorOutput_NodeExecutionResult_WithWarningLogger(r.ErrorCode, r.ErrorMessage);

            //code-step: 1.4 - Report progress milestone to execution context
            await ReportNodeProgress_ResourceOperation(nodeExecutionContext, "IntegrationCallCompleted");

            //code-step: 1.5 - Extract token info from result
            var token = r.Token;
            var tokenSummary = new Dictionary<string, object>
            {
                ["accessor"]    = token.Accessor,
                ["ttl"]         = token.Ttl,
                ["renewable"]   = token.Renewable,
                ["policies"]    = token.Policies,
                ["displayName"] = token.DisplayName,
            };

            //code-step: 1.6 - Build output metadata dictionary
            var resultManager = NodeResultOperateManager.CreateInstance(nodeExecutionContext);
            var outputData = resultManager.GetOrCreateOutputData();
            outputData["status"]      = "success";
            outputData["resource"]    = mySettings.Resource ?? "token";
            outputData["operation"]   = mySettings.Operation ?? "lookupSelf";
            outputData["errorCode"]   = string.Empty;
            outputData["accessor"]    = token.Accessor;
            outputData["ttl"]         = token.Ttl;
            outputData["renewable"]   = token.Renewable;
            outputData["policies"]    = token.Policies;
            outputData["displayName"] = token.DisplayName;

            //code-step: 1.7 - Convert token info to standard items array
            outputData.TryGetValue(ExecutionConstants.OutputFieldNameConstants.CONST_items, out var existingItemsValue);
            outputData[ExecutionConstants.OutputFieldNameConstants.CONST_items] = ApplyOutputItemsMerge(existingItemsValue, WrapSingleObjectIntoItems(tokenSummary, nodeExecutionContext));

            //code-step: 1.8 - Write output (handles TargetDataPath writes + items downstream)
            return await WriteOutputData(ExecutionConstants.OutputPorts.Success, outputData, tokenSummary, nodeExecutionContext, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            //code-step: 1.9 - Catch unexpected exceptions and return error with context
            return GetBuildErrorOutput_NodeExecutionResult_WithLogging("HASHICORP_UPSTREAM_ERROR", $"token/lookupSelf failed: {ex.Message}", ex);
        }
    }
}
