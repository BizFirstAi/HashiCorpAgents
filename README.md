# HashiCorpAgents

[![BizFirst.Ai](https://www.bizfirstai.com/website/assets/Logo/logo.png)](https://bizfirstai.com)

HashiCorp community node for [BizFirst.Ai](https://bizfirstai.com) — a ProcessEngine `ExecutionNode`
(`hashicorp`) that exposes a tenant-operated **HashiCorp Vault** server's secrets-management API as
drag-and-drop steps in [BizFirst.Ai](https://bizfirstai.com) workflow automations.

## What it does

`HashiCorpAgents` lets a BizFirst.Ai workflow talk to a tenant's own HashiCorp Vault server
(self-hosted OSS/Enterprise, or HCP Vault Dedicated) over its REST HTTP API — KV secrets (v1/v2), Vault
client-token lifecycle, dynamic-secret lease management, and unauthenticated health/seal-status
pre-flight checks. There is no BizFirst-hosted Vault and no default server address; every operation is
pointed at the tenant's own Vault via the `vaultAddress` config key.

> **Folder note:** this node lives under the platform's `Blockchain` ExecutionNodes folder for
> repo-organization reasons only. HashiCorp's actual product here is **Vault** — nothing in this
> repository talks to a blockchain, and no Terraform or Consul integration exists in this codebase.

| Resource | Operation | Description |
|---|---|---|
| `secrets` | `read` | Read a secret's data at an optional specific version (v2) or current value (v1). |
| `secrets` | `write` | Write a flat key/value map; `cas` enables Check-And-Set on v2. |
| `secrets` | `delete` | Soft delete on v2 (specified/latest versions); immediately permanent on v1. |
| `secrets` | `undelete` | v2 only. Restore soft-deleted versions. |
| `secrets` | `destroy` | v2 only. Permanently destroy versions — no undo. |
| `secrets` | `list` | List keys under a path. Empty folder is a valid empty-list success. |
| `secrets` | `readMetadata` | v2 only. Version history, `cas_required`, `max_versions`. |
| `secrets` | `updateMetadata` | v2 only. Configure retention without writing new data. |
| `token` | `lookupSelf` | This token's TTL, policies, renewable flag. Mutates nothing. |
| `token` | `lookupByAccessor` | Inspect a different token by accessor, without needing its raw value. |
| `token` | `renewSelf` | Extend this token's own TTL. |
| `token` | `renewByAccessor` | Extend a delegated/child token's TTL. |
| `token` | `revokeByAccessor` | Revoke a short-lived child token. |
| `lease` | `renew` | Extend a lease on a dynamic secret issued by another engine (database/AWS/PKI). |
| `lease` | `revoke` | Immediately invalidate a lease. |
| `lease` | `lookup` | Check a lease's remaining TTL/renewability. |
| `system` | `health` | Unauthenticated. Normalizes Vault's `/sys/health` status-code table into a decoded cluster state. |
| `system` | `sealStatus` | Unauthenticated. Answers even when Vault is sealed — pre-flight check before authenticated calls. |

Every authenticated operation accepts `vaultAddress`, `authMethod` (`token` \| `appRole`),
`credentialID`, plus optional `appRolePath` (default `approle`) and `namespace` (Vault Enterprise). The
two `system` operations skip authentication entirely.

## Source Code

Browse the implementation directly under [`src/`](src/):

- [`src/BizFirst.Integration.HashiCorp.Domain`](src/BizFirst.Integration.HashiCorp.Domain) — 18 result records + shared value types, zero project references.
- [`src/BizFirst.Integration.HashiCorp.Services`](src/BizFirst.Integration.HashiCorp.Services) — Vault HTTP client, AppRole/Token auth + caching, one service class per resource.
- [`src/BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp`](src/BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp) — the ExecutionNode itself: routing, config parsing, DI registration.

## Documentation

| Page | Published URL |
|---|---|
| Operation reference | https://bizfirstai.github.io/HashiCorpAgents/ |
| Guide: Overview | https://bizfirstai.github.io/HashiCorpAgents/guide/ |
| Guide: Configuration | https://bizfirstai.github.io/HashiCorpAgents/guide/01-configuration.html |
| Guide: Authentication | https://bizfirstai.github.io/HashiCorpAgents/guide/02-authentication.html |
| Guide: Secrets Operations | https://bizfirstai.github.io/HashiCorpAgents/guide/03-secrets-operations.html |
| Guide: Token Operations | https://bizfirstai.github.io/HashiCorpAgents/guide/04-token-operations.html |
| Guide: Lease Operations | https://bizfirstai.github.io/HashiCorpAgents/guide/05-lease-operations.html |
| Guide: System Operations | https://bizfirstai.github.io/HashiCorpAgents/guide/06-system-operations.html |
| Guide: Input & Output | https://bizfirstai.github.io/HashiCorpAgents/guide/07-input-output.html |
| Guide: Error Codes | https://bizfirstai.github.io/HashiCorpAgents/guide/08-error-codes.html |
| Guide: Examples | https://bizfirstai.github.io/HashiCorpAgents/guide/09-examples.html |
| Guide: Troubleshooting | https://bizfirstai.github.io/HashiCorpAgents/guide/10-troubleshooting.html |
| Guide: Roadmap | https://bizfirstai.github.io/HashiCorpAgents/guide/11-roadmap.html |

Same guide, also published in the portal: [bizfirstai.github.io/UserGuides/Nodes/HashiCorp](https://bizfirstai.github.io/UserGuides/Nodes/HashiCorp/)
Full developer portal: [docs.bizfirstai.com](https://docs.bizfirstai.com)

## Project layout

```
src/
├── BizFirst.Integration.HashiCorp.Domain            # Result records + shared value types (zero deps)
├── BizFirst.Integration.HashiCorp.Services           # Vault HTTP client + AppRole/Token auth + resource services
└── BizFirst.Ai.ExecutionNodes.Blockchain.HashiCorp    # Executor: routing, config, operation DTOs
```

Targets **.NET 9**.

## Configuration

There is no fixed Vault server baked into application settings — every call carries its own
`vaultAddress`:

```json
// Per-operation config, not appsettings.json — Vault is self-hosted per tenant
{
  "resource": "secrets",
  "operation": "read",
  "vaultAddress": "https://vault.internal.example.com:8200",
  "authMethod": "token",
  "credentialID": 42,
  "mount": "secret",
  "path": "myapp/db-creds"
}
```

`HashiCorpApiClientOptions` configures HTTP retry behavior only: `MaxRetries` (default 2) and
`InitialRetryDelay` (default 1s, doubling per attempt).

## Registration

`HashiCorpDependency.RegisterDefaults(services)` registers the Vault HTTP client, per-resource services,
the executor (scoped), and the `ExecutorRegistry` entry (`hashicorp`). Host applications must also add
`new HashiCorpDependency().RegisterDefaults(services);` to their node-plugin bootstrap — per this
codebase's assembly-scanning plugin loader, a `ProjectReference` alone does not guarantee the assembly is
loaded when `RegisterNodeExecutors()` runs.

## Roadmap

- **Open decision — single-use AppRole `secret_id`:** if a tenant's AppRole uses
  `secret_id_num_uses = 1`, the current cache-then-reauth design works until the cached client token
  expires, then fails every subsequent call with `HASHICORP_AUTH_FAILED`. Use `secret_id_num_uses = 0`
  until this is resolved with either a hard prerequisite or a re-issuance mechanism.
- **Transit (encrypt/decrypt) resource** — flagged as a Phase 2 candidate, not committed scope.
- **Live integration testing** against a real Vault instance (OSS, Enterprise, HCP Vault Dedicated) —
  everything today is verified by compiling and by direct comparison against HashiCorp's official API
  docs, not by exercising a running server.
- Deliberately **not** planned: `token/create` (admin operation, out of scope), Kubernetes/LDAP/Okta/JWT/
  cert auth methods (only Token and AppRole are supported), and response caching for Secrets reads
  (a security anti-pattern, not a gap).

## About BizFirst.Ai

[BizFirst.Ai](https://bizfirstai.com) is a workflow automation platform for building AI-driven business
processes. This node is one of many community connectors that plug into its ProcessEngine — browse the
full node catalogue and developer guides at [docs.bizfirstai.com](https://docs.bizfirstai.com), or join
the discussion at [community.bizfirstai.com](https://community.bizfirstai.com).

## License

Community node maintained by the [BizFirst.Ai](https://bizfirstai.com) team.
