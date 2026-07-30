using BizFirst.Ai.ProcessEngine.Service;
namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

/// <summary>
/// DI registration for the HashiCorp Vault ExecutionNode plugin. Registers all HashiCorp Vault
/// services, the executor, and the executor registry entry.
///
/// IMPORTANT — this alone is not sufficient to make the node discoverable at runtime. Per this
/// codebase's plugin-loading mechanism (BizFirst.Ai.Platform.Web.Server.Core/DependencyInjection/
/// Ai/ServiceCollectionExtensionsForAI.cs), an assembly that is only ProjectReference'd but never
/// force-loaded is not guaranteed to be present in AppDomain.CurrentDomain.GetAssemblies() when the
/// assembly-scanning RegisterNodeExecutors() runs — see the explicit
/// "new HashiCorpDependency().RegisterDefaults(services);" line added to Plugins_RegisterAllNodes(...)
/// in that file, alongside Ethereum's and IPFS's identical registrations.
/// </summary>
public sealed class HashiCorpDependency : INodeExecutorDependency
{
    public void RegisterDefaults(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));

        services.AddDependenciesHashiCorpServices();
        services.AddScoped<HashiCorpNodeExecutor>();
        ExecutorRegistry.Register<HashiCorpNodeExecutor>(HashiCorpNodeExecutor.NodeTypeName);
    }
}
