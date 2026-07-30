# HashiCorp Vault ExecutionNode — Development History

## Initial build (2026-07-21)
- `HashiCorpNodeExecutor` — routing for 18 operations across 4 resources (`NodeTypeName = "hashicorp"`, Area `Blockchain`, matching the `Ethereum`/`IPFS` sibling convention)
- `BaseHashiCorpOperationInfo` / `HashiCorpOperationInfoFactory` — config-parsing layer, universal keys (`vaultAddress`, `authMethod`, `credentialID`, `appRolePath`, `namespace`) plus per-resource keys
- Two credential shapes (Token via `GetBearerTokenAsync`, AppRole via `GetPasswordAsync` + `HashiCorpAuthClient`) resolved through `IHashiCorpCredentialResolver`; `system/health`/`system/sealStatus` deliberately skip credential resolution entirely (unauthenticated)
- `HashiCorpDependency : INodeExecutorDependency` — also requires an explicit force-load registration line in the platform host (`ServiceCollectionExtensionsForAI.cs`)
- Feature partials follow the platform's live 9-step `//code-step:` convention (Guideline 14), confirmed against real Slack/Ethereum/Docker code, not just guideline prose
- Implements `010_NodeDesign-Engineer/ExecutionNodes/HashiCorp/44_Features/Design/00_INDEX.md`, including its three review passes' correctness findings (standby-vs-sealed status codes, LIST-on-empty-path, AppRole single-use `secret_id`, `cas:0` semantics, `appRolePath` threading)
