using BizFirst.Ai.ProcessEngine.Service;
using BizFirst.Integration.HashiCorp.Domain;
using BizFirst.Integration.HashiCorp.Services.Auth;

namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

/// <summary>
/// Resolves the node's own Vault authentication material (never a secret Vault manages — see the
/// design docs §1.3) and builds the <see cref="HashiCorpCallContext"/> every authenticated Secrets/
/// Token/Lease feature partial passes down to its service. <c>system/health</c> and
/// <c>system/sealStatus</c> never call this — they are unauthenticated and build their own context
/// directly from <see cref="BaseHashiCorpOperationInfo.VaultAddress"/>.
/// </summary>
public sealed partial class HashiCorpNodeExecutor
{
    private async Task<(HashiCorpCallContext? Context, NodeExecutionResult? Error)> _ResolveCallContextAsync(
        BaseHashiCorpOperationInfo info, CancellationToken cancellationToken)
    {
        string clientToken;
        try
        {
            clientToken = await _hashiCorpCredentialResolver.ResolveClientTokenAsync(
                info.CredentialID!.Value, info.AuthMethod!, info.VaultAddress!, info.AppRolePath, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (HashiCorpConfigurationException ex)
        {
            var errorCode = info.AuthMethod == "token" ? "HASHICORP_CREDENTIAL_NOT_FOUND" : "HASHICORP_AUTH_FAILED";
            return (null, GetBuildErrorOutput_NodeExecutionResult_WithWarningLogger(errorCode, ex.Message));
        }

        var ctx = new HashiCorpCallContext(info.VaultAddress!, clientToken, info.Mount, info.EngineVersion, info.Namespace);
        return (ctx, null);
    }
}
