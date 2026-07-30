# HashiCorp Vault Services — Development History

## Initial build (2026-07-21)
- `HashiCorpApiClient` — no fixed base URL (Vault is self-hosted per tenant); retry/backoff on 429/5xx, never on 503 (sealed); special-cases LIST returning 404-on-empty per Vault's own documented convention
- `HashiCorpSystemService` — normalizes `/sys/health`'s full documented status-code table (200/429/472/473/474/501/503/530) into a successful result carrying decoded cluster state; defaults `standbyok=true&perfstandbyok=true`
- `HashiCorpAuthClient`/`HashiCorpCredentialResolver` — AppRole login takes a configurable `appRolePath` (not hardcoded `/v1/auth/approle/login`); Token auth via the existing `ICredentialResolver.GetBearerTokenAsync`
- No response caching for Secrets reads — a deliberate divergence, not an oversight (caching a secrets-manager response is a security anti-pattern)
