using BizFirst.Ai.ProcessEngine.Service;
namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

using BizFirst.Ai.ProcessEngine.Domain.Credentials;
using BizFirst.Integration.HashiCorp.Services;
using BizFirst.Integration.HashiCorp.Services.Auth;

/// <summary>
/// HashiCorp Vault ExecutionNode — routes every execution to the correct feature partial based on
/// the <c>resource</c>/<c>operation</c> config keys resolved by <see cref="HashiCorpNodeExecutorSettings"/>
/// via <see cref="HashiCorpOperationInfoFactory"/>. See the design docs
/// (010_NodeDesign-Engineer/ExecutionNodes/HashiCorp) for the full 4-resource/18-operation catalog
/// this implements, including the three review passes' correctness findings (standby-vs-sealed
/// status codes, LIST-on-empty-path, AppRole single-use secret_id, cas:0 semantics).
///
/// Design:
///   HashiCorpNodeExecutor.cs             — routing + constructor        (this file)
///   HashiCorpNodeExecutor.Config.cs       — settings accessor + port initialisation + manifest
///   HashiCorpNodeExecutor.Credentials.cs  — Vault auth resolution + call-context building
///   Main/Executor/Features/{Resource}/{Operation}/ — one feature partial per operation
///
/// Registration: BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp.HashiCorpDependency (Support/).
/// </summary>
public sealed partial class HashiCorpNodeExecutor : ResourceBasedNodeExecutor, IActionNodeExecution
{
    public const string NodeTypeName = "hashicorp";
    public override string ProcessElementTypeCode => NodeTypeName;

    private readonly HashiCorpSecretService _secretService;
    private readonly HashiCorpTokenService _tokenService;
    private readonly HashiCorpLeaseService _leaseService;
    private readonly HashiCorpSystemService _systemService;
    private readonly IHashiCorpCredentialResolver _hashiCorpCredentialResolver;

    public HashiCorpNodeExecutor(
        ILogger<HashiCorpNodeExecutor> logger,
        HashiCorpSecretService secretService,
        HashiCorpTokenService tokenService,
        HashiCorpLeaseService leaseService,
        HashiCorpSystemService systemService,
        IHashiCorpCredentialResolver credentialResolver,
        INodeCredentialsFactory? credentialsFactory = null,
        INodeConfigurationParser? configParser = null,
        NodeFormResolver? formResolver = null)
        : base(logger, credentialsFactory, configParser, formResolver)
    {
        _secretService = secretService ?? throw new ArgumentNullException(nameof(secretService));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _leaseService = leaseService ?? throw new ArgumentNullException(nameof(leaseService));
        _systemService = systemService ?? throw new ArgumentNullException(nameof(systemService));
        _hashiCorpCredentialResolver = credentialResolver ?? throw new ArgumentNullException(nameof(credentialResolver));
    }

    protected override async Task<NodeExecutionResult> _ExecuteInternal_Route_Async(
        NodeExecutionContext nodeExecutionContext,
        CancellationToken cancellationToken = default)
    {
        var result = (mySettings!.Resource, mySettings!.Operation) switch
        {
            // ── secrets (8) ──────────────────────────────────────────────────
            ("secrets", "read")           => await _HashiCorp_Secrets_ReadAsync(nodeExecutionContext, cancellationToken),
            ("secrets", "write")          => await _HashiCorp_Secrets_WriteAsync(nodeExecutionContext, cancellationToken),
            ("secrets", "delete")         => await _HashiCorp_Secrets_DeleteAsync(nodeExecutionContext, cancellationToken),
            ("secrets", "undelete")       => await _HashiCorp_Secrets_UndeleteAsync(nodeExecutionContext, cancellationToken),
            ("secrets", "destroy")        => await _HashiCorp_Secrets_DestroyAsync(nodeExecutionContext, cancellationToken),
            ("secrets", "list")           => await _HashiCorp_Secrets_ListAsync(nodeExecutionContext, cancellationToken),
            ("secrets", "readMetadata")   => await _HashiCorp_Secrets_ReadMetadataAsync(nodeExecutionContext, cancellationToken),
            ("secrets", "updateMetadata") => await _HashiCorp_Secrets_UpdateMetadataAsync(nodeExecutionContext, cancellationToken),

            // ── token (5) ────────────────────────────────────────────────────
            ("token", "lookupSelf")       => await _HashiCorp_Token_LookupSelfAsync(nodeExecutionContext, cancellationToken),
            ("token", "lookupByAccessor") => await _HashiCorp_Token_LookupByAccessorAsync(nodeExecutionContext, cancellationToken),
            ("token", "renewSelf")        => await _HashiCorp_Token_RenewSelfAsync(nodeExecutionContext, cancellationToken),
            ("token", "renewByAccessor")  => await _HashiCorp_Token_RenewByAccessorAsync(nodeExecutionContext, cancellationToken),
            ("token", "revokeByAccessor") => await _HashiCorp_Token_RevokeByAccessorAsync(nodeExecutionContext, cancellationToken),

            // ── lease (3) ────────────────────────────────────────────────────
            ("lease", "renew")  => await _HashiCorp_Lease_RenewAsync(nodeExecutionContext, cancellationToken),
            ("lease", "revoke") => await _HashiCorp_Lease_RevokeAsync(nodeExecutionContext, cancellationToken),
            ("lease", "lookup") => await _HashiCorp_Lease_LookupAsync(nodeExecutionContext, cancellationToken),

            // ── system (2) — unauthenticated ─────────────────────────────────
            ("system", "health")     => await _HashiCorp_System_HealthAsync(nodeExecutionContext, cancellationToken),
            ("system", "sealStatus") => await _HashiCorp_System_SealStatusAsync(nodeExecutionContext, cancellationToken),

            _ => await base._ExecuteInternal_Route_Async(nodeExecutionContext, cancellationToken)
        };

        LogActivity_Status($"{NodeTypeName} operation executed", result.IsSuccess);
        return result;
    }
}
