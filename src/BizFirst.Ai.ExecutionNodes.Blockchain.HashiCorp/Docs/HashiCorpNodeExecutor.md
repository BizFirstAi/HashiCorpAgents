# HashiCorp Vault ExecutionNode — Operation Reference

`NodeTypeName`: `hashicorp` · Area: `Blockchain`

Every operation takes `resource`, `operation`, `vaultAddress`, and — except for the two unauthenticated
`system` operations — `authMethod` (`"token"` \| `"appRole"`) and `credentialID`. `appRolePath`
(default `"approle"`) and `namespace` (Vault Enterprise) are optional on every authenticated operation.
`*` marks a required operation-specific field.

## secrets (KV v1/v2)

`mount`* and `engineVersion` (`"1"` \| `"2"`, default `"2"`) apply to every operation in this resource.

| operation | fields | notes |
|---|---|---|
| `read` | `path`*, `version` | `version` only meaningful on v2 (historical version read) |
| `write` | `path`*, `secretData`*, `cas` | `cas: 0` guards against overwriting an existing key (v2 only) |
| `delete` | `path`*, `versions` | Soft delete (specified or latest) on v2; **immediately permanent** on v1 |
| `undelete` | `path`*, `versions`* | v2 only. Restores soft-deleted versions |
| `destroy` | `path`*, `versions`* | v2 only. Permanent — no undo, unlike `delete` |
| `list` | `path` | Empty folder is a valid empty-list success, never an error |
| `readMetadata` | `path`* | v2 only. Version history, `cas_required`, `max_versions` |
| `updateMetadata` | `path`*, `maxVersions`, `casRequired` | v2 only. Configures retention without writing new data |

## token

Deliberately scoped to what an automation credential should do — no `token/create` (admin operation, out of scope).

| operation | fields | notes |
|---|---|---|
| `lookupSelf` | — | No path parameter, mutates nothing — the credential-health-check candidate |
| `lookupByAccessor` | `accessor`* | Inspect a different token without needing its raw value |
| `renewSelf` | `incrementSeconds` | Vault may cap the returned TTL below the request |
| `renewByAccessor` | `accessor`*, `incrementSeconds` | Extends a delegated/child token's TTL |
| `revokeByAccessor` | `accessor`* | Cleans up a short-lived child token |

## lease

Manages leases on dynamic secrets issued by some *other* engine (database/AWS/PKI) earlier in the
workflow — this resource does not itself provide a way to create a lease.

| operation | fields | notes |
|---|---|---|
| `renew` | `leaseID`*, `incrementSeconds` | |
| `revoke` | `leaseID`* | Immediately invalidates the lease |
| `lookup` | `leaseID`* | Check remaining TTL/renewability before deciding whether to renew |

## system

Both operations are **unauthenticated** — `authMethod`/`credentialID` are omitted entirely.

| operation | fields | notes |
|---|---|---|
| `health` | — | Normalizes Vault's full documented `/sys/health` status-code table (200/429/472/473/474/501/503/530) into a successful result carrying the decoded cluster state; only a genuine network failure produces an error |
| `sealStatus` | — | Answers even when Vault is sealed — useful as a pre-flight check before any authenticated operation, which would otherwise fail with `HASHICORP_SEALED` |

## Error codes

`HASHICORP_INVALID_CONFIGURATION`, `HASHICORP_CREDENTIAL_NOT_FOUND`, `HASHICORP_AUTH_FAILED`,
`HASHICORP_TOKEN_EXPIRED`, `HASHICORP_PERMISSION_DENIED`, `HASHICORP_SECRET_NOT_FOUND`,
`HASHICORP_CAS_MISMATCH`, `HASHICORP_LEASE_NOT_FOUND`, `HASHICORP_LEASE_NOT_RENEWABLE`,
`HASHICORP_SEALED`, `HASHICORP_VAULT_UNREACHABLE`, `HASHICORP_UPSTREAM_ERROR`.
