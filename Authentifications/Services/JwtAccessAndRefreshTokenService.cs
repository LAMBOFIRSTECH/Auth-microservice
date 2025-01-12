using System.IdentityModel.Tokens.Jwt;
using System.Net.Sockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Authentifications.Interfaces;
using Authentifications.Models;
using Microsoft.IdentityModel.Tokens;
using VaultSharp;
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

    public async Task<TokenResult> NewAccessTokenUsingRefreshTokenInRedisAsync(string refresh, string email, string password)
    {
        var utilisateurDto = await AuthUserDetailsAsync((true, email, password));
        var refreshTokenFromRedis = await redisTokenCache.RetrieveTokenBasingOnRedisUserSessionAsync(utilisateurDto.Email!, utilisateurDto.Pass!);
        if (string.IsNullOrEmpty(refreshTokenFromRedis))
            throw new Exception("Empty refresh token retrieve from redis");

        if (!refreshTokenFromRedis.Equals(refresh))
            throw new Exception("Not the same refresh token");
        GetToken(utilisateurDto);
        return GetToken(utilisateurDto); ;

    }
    public TokenResult GetToken(UtilisateurDto utilisateurDto)
    {
        log.LogInformation("Création du token de session pour l'utilisateur : {utilisateur.Email}", utilisateurDto.Email);
        var result = GenerateJwtTokenAndStatefulRefreshToken(utilisateurDto);
        result.Response = true;
        if (string.IsNullOrWhiteSpace(result.RefreshToken))
            throw new Exception("Le couple token et refreshToken est vide.");
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
        StorePublicKeyInVault(publicKey);
        rsaSecurityKey = new RsaSecurityKey(rsa.ExportParameters(true));
        return rsaSecurityKey;
    }
    public static string ConvertToPem(byte[] keyBytes, string keyType)
    {
        var base64Key = Convert.ToBase64String(keyBytes);
        var sb = new StringBuilder();
        sb.AppendLine($"-----BEGIN {keyType}-----");
        int lineLength = 64;
        for (int i = 0; i < base64Key.Length; i += lineLength)
        {
            sb.AppendLine(base64Key.Substring(i, Math.Min(lineLength, base64Key.Length - i)));
        }
        sb.AppendLine($"-----END {keyType}-----");
        return sb.ToString();
    }
    private async void StorePublicKeyInVault(string publicKeyPem)
    {
        var hashiCorpToken = configuration["HashiCorp:VaultToken"];
        var hashiCorpHttpClient = configuration["HashiCorp:HttpClient:BaseAddress"];
        var secretPath = configuration["HashiCorp:SecretsPath"];
        if (string.IsNullOrEmpty(hashiCorpToken) || string.IsNullOrEmpty(hashiCorpHttpClient) || string.IsNullOrEmpty(secretPath))
        {
            log.LogWarning("La configuration de HashiCorp Vault est manquante ou invalide.");
            throw new InvalidOperationException("La configuration de HashiCorp Vault est manquante ou invalide.");
        }
        var vaultClientSettings = new VaultClientSettings($"{hashiCorpHttpClient}", new TokenAuthMethodInfo(hashiCorpToken));
        var vaultClient = new VaultClient(vaultClientSettings);
        try
        {
        await vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(
        secretPath, new Dictionary<string, object>
        {
            { "authenticationSignatureKey", publicKeyPem }
        });
            log.LogInformation("Successfull storage public key Vault !");
        }
        catch (Exception ex) when (ex.InnerException is SocketException socket)
        {
            log.LogError("Socket's problems check if TasksManagement service is UP: {socket.Message}", socket.Message);
            throw new Exception("The service is unavailable. Please retry soon.", ex);
        }
    }
    public TokenResult GenerateJwtTokenAndStatefulRefreshToken(UtilisateurDto utilisateurDto)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var additionalAudiences = new[] { "https://localhost:7082", "https://audience2.com", "https://localhost:9500", "https://192.168.153.131:7250", "https://audience1.com" };
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
        // Revocation d'un token dans quelle mesure A revoir maybe dans l'autre microservice pour les utilisateurs avec Policy=UserPolicy
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
        log.LogInformation("Authentication successful {Parameter.Item1}", Parameter.Item1);
        return Parameter.Item2;
    }
}