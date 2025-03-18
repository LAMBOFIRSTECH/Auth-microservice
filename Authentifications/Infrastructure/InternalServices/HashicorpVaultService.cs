using System.Net.Sockets;
using Authentifications.Application.Exceptions;
using Authentifications.Core.Interfaces;
using Authentifications.Core.Entities;
using VaultSharp;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.AuthMethods.Token;
namespace Authentifications.Infrastructure.InternalServices;
public class HashicorpVaultService : IHashicorpVaultService
{
    private readonly IConfiguration configuration;
    private readonly ILogger<HashicorpVaultService> log;
    public HashicorpVaultService(IConfiguration configuration, ILogger<HashicorpVaultService> log)
    {
        this.configuration = configuration;
        this.log = log;
    }
    public async Task<string> GetAppRoleTokenFromVault()
    {
        var hashiCorpRoleID = configuration["HashiCorp:AppRole:RoleID"];
        var hashiCorpSecretID = configuration["HashiCorp:AppRole:SecretID"];
        var hashiCorpHttpClient = configuration["HashiCorp:HttpClient:BaseAddress"];
        if (string.IsNullOrEmpty(hashiCorpRoleID) || string.IsNullOrEmpty(hashiCorpSecretID) || string.IsNullOrEmpty(hashiCorpHttpClient))
        {
            log.LogWarning("💢 Empty or invalid HashiCorp Vault configurations.");
            throw new VaultConfigurationException(500, "Warning", "💢 Empty or invalid HashiCorp Vault configurations.");
        }
        var appRoleAuthMethodInfo = new AppRoleAuthMethodInfo(hashiCorpRoleID, hashiCorpSecretID);
        var vaultClientSettings = new VaultClientSettings($"{hashiCorpHttpClient}", appRoleAuthMethodInfo);
        var vaultClient = new VaultClient(vaultClientSettings);
        try
        {
            var authResponse = await vaultClient.V1.Auth.AppRole.LoginAsync(appRoleAuthMethodInfo);
            string token = authResponse.AuthInfo.ClientToken;
            if (string.IsNullOrEmpty(token))
                throw new InvalidOperationException("💢 Empty token retrieve from HashiCorp Vault");
            return token;
        }
        catch (Exception ex) when (ex.InnerException is SocketException socket)
        {
            log.LogError(socket, "💢 Socket's problems check if Hashicorp Vault server is UP");
            throw new InvalidOperationException("💢 The service is unavailable. Please retry soon.", ex);
        }
    }
    public async Task<string> GetLdapPassWordFromVault()
    {
        string vautlAppRoleToken = await GetAppRoleTokenFromVault();
        string secretPath = configuration["Ldap:LdapSecretPath"]!;
        var hashiCorpHttpClient = configuration["HashiCorp:HttpClient:VaultAddress"];
        if (string.IsNullOrEmpty(hashiCorpHttpClient) || string.IsNullOrEmpty(secretPath))
        {
            log.LogWarning("Empty or invalid HashiCorp Vault configurations.");
            throw new VaultConfigurationException(500, "Warning", "Empty or invalid HashiCorp Vault configurations.");
        }
        var vaultClientSettings = new VaultClientSettings($"{hashiCorpHttpClient}", new TokenAuthMethodInfo(vautlAppRoleToken));
        var vaultClient = new VaultClient(vaultClientSettings);
        var secret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(secretPath);
        if (secret == null)
        {
            log.LogError("Le secret Vault est introuvable pour la chaine de connection Ldap.");
            throw new VaultConfigurationException(404, "Error", "Le secret Vault est introuvable.");
        }
        var secretData = secret.Data.Data;
        if (!secretData.ContainsKey("ldapPassword"))
        {
            log.LogError("La clé 'ldapPassword' est manquante dans le secret Vault.");
            throw new VaultConfigurationException(404, "Error", "La clé 'ldapPassword' est introuvable.");
        }
        return secretData["ldapPassword"].ToString()!;
    }
    public async Task<string> GetRabbitConnectionStringFromVault()
    {
        string vautlAppRoleToken = await GetAppRoleTokenFromVault();
        var secretPath = configuration["HashiCorp:RabbitMqPath"];
        var hashiCorpHttpClient = configuration["HashiCorp:HttpClient:BaseAddress"];
        if (string.IsNullOrEmpty(hashiCorpHttpClient) || string.IsNullOrEmpty(secretPath))
        {
            log.LogWarning("💢 Empty or invalid HashiCorp Vault configurations.");
            throw new VaultConfigurationException(500, "Warning", "💢 Empty or invalid HashiCorp Vault configurations.");
        }
        var vaultClientSettings = new VaultClientSettings($"{hashiCorpHttpClient}", new TokenAuthMethodInfo(vautlAppRoleToken));
        var vaultClient = new VaultClient(vaultClientSettings);
        var secret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(secretPath);
        if (secret == null)
        {
            log.LogError("Le secret Vault est introuvable pour la chaine de connection rabbitMQ.");
            throw new VaultConfigurationException(404, "Error", "Le secret Vault est introuvable.");
        }
        var secretData = secret.Data.Data;
        if (!secretData.ContainsKey("rabbitMqConnectionString"))
        {
            log.LogError("❌ La clé 'rabbitMqConnectionString' est manquante dans le secret Vault.");
            throw new VaultConfigurationException(404, "Error", "❌ Key 'rabbitMqConnectionString' not found.");
        }
        return secretData["rabbitMqConnectionString"].ToString()!;
    }
    public async Task<Message> StoreJwtPublicKeyInVault(string publicKeyPem)
    {
        string vautlAppRoleToken = await GetAppRoleTokenFromVault();
        var secretPath = configuration["HashiCorp:JwtPublicKeyPath"];
        var hashiCorpHttpClient = configuration["HashiCorp:HttpClient:BaseAddress"];
        if (string.IsNullOrEmpty(hashiCorpHttpClient) || string.IsNullOrEmpty(secretPath))
        {
            log.LogWarning("💢 Empty or invalid HashiCorp Vault configurations.");
            throw new VaultConfigurationException(500, "Warning", "Empty or invalid HashiCorp Vault configurations.");
        }
        var vaultClientSettings = new VaultClientSettings($"{hashiCorpHttpClient}", new TokenAuthMethodInfo(vautlAppRoleToken));
        var vaultClient = new VaultClient(vaultClientSettings);
        await vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(
        secretPath, new Dictionary<string, object> { { "authenticationSignatureKey", publicKeyPem } });
        return new Message
        {
            Title = "Hashicorp Vault configuration",
            Type = "Succes",
            Detail = "✅ Successfull storage public key Vault !",
            Status = 200,
        };
    }
}