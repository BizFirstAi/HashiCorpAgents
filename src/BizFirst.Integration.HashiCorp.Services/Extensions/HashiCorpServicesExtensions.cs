using BizFirst.Integration.HashiCorp.Services.Auth;
using BizFirst.Integration.HashiCorp.Services.Http;

namespace BizFirst.Integration.HashiCorp.Services.DependencyInjection;

/// <summary>Registers all HashiCorp Vault integration services in the DI container.</summary>
public static class HashiCorpServicesExtensions
{
    /// <summary>
    /// Registers the HTTP client, AppRole auth client, credential resolver, and all four resource
    /// services. No base address is configured on the typed clients — Vault is self-hosted per
    /// tenant, so every call carries its own <c>vaultAddress</c> via <c>HashiCorpCallContext</c>.
    /// </summary>
    public static IServiceCollection AddDependenciesHashiCorpServices(this IServiceCollection services)
    {
        services.AddSingleton<HashiCorpApiClientOptions>();

        services.AddHttpClient<HashiCorpApiClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddScoped<IHashiCorpAuthClient, HashiCorpAuthClient>();
        services.AddScoped<IHashiCorpCredentialResolver, HashiCorpCredentialResolver>();

        // Singleton so cached AppRole tokens survive across the scoped resolver's per-execution
        // lifetime — a per-execution cache would never produce a hit. See IHashiCorpAppRoleTokenCache.
        services.AddSingleton<IHashiCorpAppRoleTokenCache, HashiCorpAppRoleTokenCache>();

        services.AddScoped<HashiCorpSecretService>();
        services.AddScoped<HashiCorpTokenService>();
        services.AddScoped<HashiCorpLeaseService>();
        services.AddScoped<HashiCorpSystemService>();

        return services;
    }
}
