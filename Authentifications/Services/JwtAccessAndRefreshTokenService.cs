using System.IdentityModel.Tokens.Jwt;
using System.Net.Sockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Authentifications.Interfaces;
using Authentifications.Models;
using Microsoft.IdentityModel.Tokens;
using VaultSharp;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.AuthMethods.Token;
namespace Authentifications.Services;
public class JwtAccessAndRefreshTokenService : IJwtAccessAndRefreshTokenService
{
    private readonly IConfiguration configuration;
    private readonly ILogger<JwtAccessAndRefreshTokenService> log;
    private readonly IRedisCacheService redisCache;
    private readonly IRedisCacheTokenService redisTokenCache;
    private RsaSecurityKey rsaSecurityKey;
    private readonly string refreshToken;

    public JwtAccessAndRefreshTokenService(IConfiguration configuration, ILogger<JwtAccessAndRefreshTokenService> log, IRedisCacheService redisCache, IRedisCacheTokenService redisTokenCache)
    {
        this.configuration = configuration;
        this.log = log;
        this.redisCache = redisCache;
        this.redisTokenCache = redisTokenCache;
        rsaSecurityKey = GetOrCreateSigningKey();
        refreshToken = GenerateRefreshToken();
    }
    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[128];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public async Task<TokenResult> NewAccessTokenUsingRefreshTokenInRedisAsync(string refreshToken, string email, string password)
    {
        var utilisateurDto = await AuthUserDetailsAsync((true, email, password));
        var refreshTokenFromRedis = await redisTokenCache.RetrieveTokenBasingOnRedisUserSessionAsync(utilisateurDto.Email!, utilisateurDto.Pass!);
        if (string.IsNullOrEmpty(refreshTokenFromRedis))
            throw new InvalidOperationException("Empty refresh token retrieve from redis");

        if (!refreshTokenFromRedis.Equals(refreshToken))
            throw new InvalidOperationException("Not the same refresh token");
        GetToken(utilisateurDto);
        return GetToken(utilisateurDto);
    }
    public TokenResult GetToken(UtilisateurDto utilisateurDto)
    {
        log.LogInformation("Creating current user session's Token");
        var result = GenerateJwtTokenAndStatefulRefreshToken(utilisateurDto);
        result.Response = true;
        if (string.IsNullOrWhiteSpace(result.RefreshToken))
            throw new InvalidOperationException("Empty cuple for access and refresh token.");
        redisTokenCache.StoreRefreshTokenSessionInRedis(utilisateurDto.Email!, result.RefreshToken!, utilisateurDto.Pass!);
        return result;
    }
    private RsaSecurityKey GetOrCreateSigningKey()
    {
        if (rsaSecurityKey != null)
            return rsaSecurityKey;
        var rsa = RSA.Create(2048);
        _ = ConvertToPem(rsa.ExportRSAPrivateKey(), "RSA PRIVATE KEY");
        var publicKey = ConvertToPem(rsa.ExportRSAPublicKey(), "RSA PUBLIC KEY");
        StoreJwtPublicKeyInVault(publicKey);
        rsaSecurityKey = new RsaSecurityKey(rsa.ExportParameters(true));
        return rsaSecurityKey;
    }
    public static string ConvertToPem(byte[] keyBytes, string keyType)
    {
        var base64Key = Convert.ToBase64String(keyBytes);
        var sb = new StringBuilder();
        sb.AppendLine($"-----BEGIN {keyType}-----");
        const int lineLength = 64;
        for (int i = 0; i < base64Key.Length; i += lineLength)
        {
            sb.Append(base64Key, i, Math.Min(lineLength, base64Key.Length - i)).AppendLine();
        }
        sb.AppendLine($"-----END {keyType}-----");
        return sb.ToString();
    }
    private async Task<string> GetAppRoleTokenFromVault()
    {
        var hashiCorpRoleID = configuration["HashiCorp:AppRole:RoleID"];
        var hashiCorpSecretID = configuration["HashiCorp:AppRole:SecretID"];
        var hashiCorpHttpClient = configuration["HashiCorp:HttpClient:BaseAddress"];
        if (string.IsNullOrEmpty(hashiCorpRoleID) || string.IsNullOrEmpty(hashiCorpSecretID) || string.IsNullOrEmpty(hashiCorpHttpClient))
        {
            log.LogWarning("Empty or invalid HashiCorp Vault configurations.");
            throw new InvalidOperationException("Empty or invalid HashiCorp Vault configurations.");
        }
        var appRoleAuthMethodInfo = new AppRoleAuthMethodInfo(hashiCorpRoleID, hashiCorpSecretID);
        var vaultClientSettings = new VaultClientSettings($"{hashiCorpHttpClient}", appRoleAuthMethodInfo);
        var vaultClient = new VaultClient(vaultClientSettings);
        try
        {
            var authResponse = await vaultClient.V1.Auth.AppRole.LoginAsync(appRoleAuthMethodInfo);
            string token = authResponse.AuthInfo.ClientToken;
            if (string.IsNullOrEmpty(token))
                throw new InvalidOperationException("Empty token retrieve from HashiCorp Vault");
            return token;
        }
        catch (Exception ex) when (ex.InnerException is SocketException socket)
        {
            log.LogError(socket, "Socket's problems check if Hashicorp Vault server is UP", socket.Message);
            throw new InvalidOperationException("The service is unavailable. Please retry soon.", ex);
        }
    }
    private async void StoreJwtPublicKeyInVault(string publicKeyPem)
    {
        string vautlAppRoleToken = await GetAppRoleTokenFromVault();
        var secretPath = configuration["HashiCorp:SecretsPath"];
        var hashiCorpHttpClient = configuration["HashiCorp:HttpClient:BaseAddress"];
        if (string.IsNullOrEmpty(hashiCorpHttpClient) || string.IsNullOrEmpty(secretPath))
        {
            log.LogWarning("Empty or invalid HashiCorp Vault configurations.");
            throw new InvalidOperationException("Empty or invalid HashiCorp Vault configurations.");
        }
        var vaultClientSettings = new VaultClientSettings($"{hashiCorpHttpClient}", new TokenAuthMethodInfo(vautlAppRoleToken));
        var vaultClient = new VaultClient(vaultClientSettings);
        await vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(
        secretPath, new Dictionary<string, object> { { "authenticationSignatureKey", publicKeyPem } });
        log.LogInformation("Successfull storage public key Vault !");
    }
    public TokenResult GenerateJwtTokenAndStatefulRefreshToken(UtilisateurDto utilisateurDto)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var additionalAudiences = new[] { "https://dev-management-tasks:7082", "https://audience2.com", "https://localhost:9500", "https://audience1.com" };
        // il faut récupérer la liste des  audiences depuis la configuration appsettings.json
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.Name, utilisateurDto.Nom),
                new Claim(ClaimTypes.Email, utilisateurDto.Email!),
                new Claim(ClaimTypes.Role, utilisateurDto.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            }
            ),
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = new SigningCredentials(GetOrCreateSigningKey(), SecurityAlgorithms.RsaSha512),
            Issuer = configuration.GetSection("JwtSettings")["Issuer"],
            Audience = null,
            Claims = new Dictionary<string, object>
    {
        { JwtRegisteredClaimNames.Aud, additionalAudiences }
    }
        };
        var tokenCreation = tokenHandler.CreateToken(tokenDescriptor);
        var token = tokenHandler.WriteToken(tokenCreation);
        TokenResult result = new()
        {
            Token = token,
            RefreshToken = refreshToken
        };
        return result;
    }
    public async Task<UtilisateurDto> AuthUserDetailsAsync((bool IsValid, string email, string password) tupleParameter)
    {
        await Task.Delay(50);
        var Parameter = await redisCache.GetBooleanAndUserDataFromRedisUsingParamsAsync(tupleParameter.IsValid, tupleParameter.email!, tupleParameter.password!);
        log.LogInformation("User details have been correctly retrieved from Redis cache db");
        return Parameter.Item2;
    }
}