namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

/// <summary>SYS02 — system/sealStatus. Unauthenticated — answers even when Vault is sealed.</summary>
internal sealed class SystemSealStatusInfo : BaseHashiCorpOperationInfo
{
    public override bool IsUnauthenticated => true;
}
