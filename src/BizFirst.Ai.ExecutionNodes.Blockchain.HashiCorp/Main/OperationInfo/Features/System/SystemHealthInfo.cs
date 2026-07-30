namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

/// <summary>SYS01 — system/health. Unauthenticated — no credential resolution needed at all.</summary>
internal sealed class SystemHealthInfo : BaseHashiCorpOperationInfo
{
    public override bool IsUnauthenticated => true;
}
