namespace BizFirst.Integration.HashiCorp.Services.Auth;

/// <summary>
/// Thrown by <see cref="HashiCorpCredentialResolver"/> when the node's own Vault authentication
/// material (a Vault token, or an AppRole role-id/secret-id pair) cannot be resolved or is invalid.
/// Never thrown for problems with secrets Vault itself manages — those are service-layer failures
/// reported through the normal <c>Result</c> types, not exceptions.
///
/// Every feature partial that calls <see cref="IHashiCorpCredentialResolver.ResolveClientTokenAsync"/>
/// must catch this (after the mandatory <see cref="OperationCanceledException"/> catch) and map it to
/// <c>HASHICORP_CREDENTIAL_NOT_FOUND</c> or <c>HASHICORP_AUTH_FAILED</c> rather than letting it escape.
/// </summary>
public sealed class HashiCorpConfigurationException : Exception
{
    public HashiCorpConfigurationException(string message) : base(message)
    {
    }

    public HashiCorpConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
