using BizFirst.Ai.ProcessEngine.Service;
namespace BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp;

/// <summary>Configuration management partial for <see cref="HashiCorpNodeExecutor"/>.</summary>
public sealed partial class HashiCorpNodeExecutor
{
    /// <summary>Returns a new <see cref="HashiCorpNodeExecutorSettings"/> instance so LoadConfigAsync gets typed access instead of DictionaryBasedSettings.</summary>
    public override BaseNodeExecutorSettings CreateExecutorSettings() => new HashiCorpNodeExecutorSettings();

    /// <summary>Typed settings accessor — casts this.settings to <see cref="HashiCorpNodeExecutorSettings"/>.</summary>
    private HashiCorpNodeExecutorSettings? mySettings => (HashiCorpNodeExecutorSettings?)this.settings;

    /// <summary>Initialises the standard success ("main") / error output ports on this node.</summary>
    public override void ValidateExecutorSettings()
    {
        if (mySettings?.OutputMapping is null) return;
        mySettings.OutputMapping.GetOrCreatePortSuccessAndError();
    }

    protected override NodeExecutorManifest? GetNodeExecutorManifest()
        => NodeExecutorManifest.Empty(ProcessElementTypeCode);
}
