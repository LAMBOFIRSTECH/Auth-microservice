using Authentifications.Application.Exceptions;
using Authentifications.Core.Interfaces;
using Authentifications.Core.Entities;
using Authentifications.Application.DTOs;
using Microsoft.Extensions.Caching.Memory;
using Novell.Directory.Ldap;
namespace Authentifications.Infrastructure.OpenLdap;
public class LdapService : ILdapService
{
    private readonly IConfiguration configuration;
    private readonly ILogger<ILdapService> log;
    private readonly IMemoryCache cache;
    private readonly IRedisCacheService redisCacheService;
    private readonly IHashicorpVaultService vaultService;
    public LdapService(IConfiguration configuration, ILogger<ILdapService> log, IMemoryCache cache, IRedisCacheService redisCacheService, IHashicorpVaultService vaultService)
    {
        this.log = log;
        this.configuration = configuration;
        this.cache = cache;
        this.redisCacheService = redisCacheService;
        this.vaultService = vaultService;
    }
    public async Task<(LdapConnection connection, ILdapSearchResults results)> EstablishConnection()
    {
        var ldapSection = configuration.GetSection("Ldap");
        var ldapConfig = ldapSection.Exists() ? ldapSection.Get<Dictionary<string, string>>() : new Dictionary<string, string>();

        if (ldapConfig == null || ldapConfig.Count == 0)
            throw new LdapConfigurationException(500, "Warning", "Empty LDAP configurations.");

        if (ldapConfig.Values.Any(string.IsNullOrEmpty) || ldapConfig.Keys.Any(string.IsNullOrEmpty))
        {
            log.LogWarning("⚠️ Invalid LDAP configurations.");
            throw new LdapConfigurationException(500, "Warning", "Invalid LDAP configurations.");
        }

        if (!short.TryParse(ldapConfig["Port"], out short ldapPort))
            throw new LdapConfigurationException(409, "Warning", "Invalid LDAP port configuration.");

        var ldapHost = ldapConfig["Host"];
        var ldapPassword = await vaultService.GetLdapPassWordFromVault();
        var ldapBaseDn = ldapConfig["BaseDn"];
        var ldapSearchBase = ldapConfig["SearchBase"];
        var ldapSearchFilter = ldapConfig["SearchFilter"];
        var ldapConn = new LdapConnection();

        try
        {
            ldapConn.Connect(ldapHost, ldapPort);
            ldapConn.ConnectionTimeout = 300000;
            ldapConn.Bind(ldapBaseDn, ldapPassword);
            if (!ldapConn.Connected)
            {
                log.LogError("❌ Échec de connexion à LDAP après Bind !");
                ldapConn.Dispose();
                return (null!, null!);
            }
            log.LogInformation("✅ Connexion réussie à LDAP");
            _ = Task.Run(async () =>
            {
                while (ldapConn.Connected)
                {
                    await Task.Delay(30000);
                    try
                    {
                        var searchRequest = new LdapSearchRequest(
                            ldapBaseDn,
                            LdapConnection.ScopeBase,
                            "(structuralObjectClass=inetOrgPerson)",
                            new[] { "entryUUID", "cn", "mail", "title", "userPassword" },
                            LdapSearchConstraints.DerefNever,
                            1,
                            0,
                            false,
                            null
                        );

                        ldapConn.SendRequest(searchRequest, null);
                        log.LogInformation("🔄 Keep-Alive envoyé à LDAP...");
                    }
                    catch (LdapException ex)
                    {
                        log.LogError("⚠️ Erreur lors du Keep-Alive LDAP : {Message}", ex.Message);
                    }
                }
            });
            var results = ldapConn.Search(
                ldapSearchBase,
                LdapConnection.ScopeSub,
                ldapSearchFilter,
                new[] { "entryUUID", "cn", "mail", "title", "userPassword" },
                false
            );

            if (results == null)
            {
                log.LogError("❌ Échec de récupération des résultats LDAP !");
                ldapConn.Dispose();
                return (null!, null!);
            }
            await Task.CompletedTask;
            return (ldapConn, results);
        }
        catch (LdapException ldapEx)
        {
            log.LogError(ldapEx, "❌ Erreur LDAP lors de la connexion :");
            ldapConn.Dispose();
            return (null!, null!);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "❌ Erreur générale:");
            ldapConn.Disconnect();
            return (null!, null!);
        }
    }
    public async Task<Message> RetrieveLdapData(string message)
    {
        var data = new List<UtilisateurDto>();
        var (ldapConnection, results) = await EstablishConnection();
        if (ldapConnection?.Connected != true)
        {
            log.LogError("❌ Impossible d'établir la connexion LDAP.");
        }
        log.LogInformation("✅ Connexion LDAP obtenue.");
        await Task.Delay(100);
        try
        {
            var userUuid = message.Split('|')[1].Trim();
            string pass = string.Empty;
            while (results.HasMore())
            {
                log.LogInformation("🔍 Job Hangfire en process - Connexion LDAP ouverte.");
                var entry = results.Next();
                if (entry == null) continue;
                var entryUuid = entry.GetAttribute("entryUUID");
                if (entryUuid == null)
                {
                    log.LogWarning("⚠️ User UUID est vide !");
                    return new Message
                    {
                        Type = "Not Found",
                        Title = "LdapService: retrieve data",
                        Detail = "⚠️ User UUID est vide !",
                        Status = 404
                    };
                }
                if (!userUuid.Equals(entryUuid.StringValue.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    log.LogWarning("💢 User UUID ne correspond pas à l'entrée LDAP !");
                    return new Message
                    {
                        Type = "Not Found",
                        Title = "LdapService: retrieve data",
                        Detail = "💢 User UUID ne correspond pas à l'entrée LDAP !",
                        Status = 404
                    };
                }
                log.LogInformation("✅ OpenLDAP user found !");
                string entryUuidString = entryUuid?.StringValue?.Trim()!;
                if (string.IsNullOrEmpty(entryUuidString))
                {
                    log.LogError("❌ entryUUID is NULL or empty !");
                    return new Message
                    {
                        Type = "Not Found",
                        Title = "LdapService: retrieve data",
                        Detail = "❌ entryUUID is NULL or empty !",
                        Status = 404
                    };
                }
                if (!Guid.TryParse(entryUuidString, out Guid guid))
                {
                    log.LogError("❌ entryUUID is not a valid GUID: {entryUuidString}", entryUuidString);
                    return new Message
                    {
                        Type = "Not Found",
                        Title = "LdapService: retrieve data",
                        Detail = $"❌ entryUUID is not a valid GUID : {entryUuidString}",
                        Status = 404
                    };
                }
                string nom = entry.GetAttribute("cn")?.StringValue?.Trim() ?? "Inconnu";
                string email = entry.GetAttribute("mail")?.StringValue?.Trim() ?? "Inconnu";
                string roleString = entry.GetAttribute("title")?.StringValue?.Trim()!;
                pass = entry.GetAttribute("userPassword")?.StringValue?.Trim() ?? "******";
                if (!Enum.TryParse(roleString, out UtilisateurDto.Privilege role))
                {
                    log.LogWarning("⚠️ Rôle LDAP non reconnu : {roleString}, valeur par défaut : Administrateur", roleString);
                    role = UtilisateurDto.Privilege.Administrateur;
                }
                data.Add(new UtilisateurDto
                {
                    ID = guid,
                    Nom = nom,
                    Email = email,
                    Role = role,
                    Pass = pass
                });
            }
            log.LogInformation(" 🗃️  Saving in memory cache RabbitMq Message and user SHA-512.");
            cache.Set("message", message, TimeSpan.FromMinutes(15));
            cache.Set("hashpass", pass, TimeSpan.FromMinutes(15));
            log.LogInformation("🎉 Sent user details for processing in Redis cache service!");
            await redisCacheService.GetUserUsingUuidAsync(data);
            return new Message
            {
                Type = "Found",
                Title = "LdapService: retrieve data",
                Detail = "🎉 Retrieving OpenLDAP data!",
                Status = 200
            };
        }
        finally
        {
            log.LogInformation("🔌 Fermeture de la connexion LDAP...");
            ldapConnection?.Disconnect();
            ldapConnection?.Dispose();
        }
    }
}