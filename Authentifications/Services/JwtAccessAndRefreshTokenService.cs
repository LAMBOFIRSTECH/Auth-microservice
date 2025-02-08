using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Authentifications.Interfaces;
using Authentifications.Models;
using Microsoft.IdentityModel.Tokens;
namespace Authentifications.Services;
public class JwtAccessAndRefreshTokenService : IJwtAccessAndRefreshTokenService
{
    private readonly IConfiguration configuration;
    private readonly ILogger<JwtAccessAndRefreshTokenService> log;
    private readonly IRedisCacheService redisCache;
    private readonly IRedisCacheTokenService redisTokenCache;
    private readonly IHashicorpVaultService hashicorpVaultService;
    private RsaSecurityKey rsaSecurityKey;
    private readonly string refreshToken;
    public JwtAccessAndRefreshTokenService(IConfiguration configuration, ILogger<JwtAccessAndRefreshTokenService> log,
    IRedisCacheService redisCache, IRedisCacheTokenService redisTokenCache, IHashicorpVaultService hashicorpVaultService)
    {
        this.configuration = configuration;
        this.log = log;
        this.redisCache = redisCache;
        this.redisTokenCache = redisTokenCache;
        this.hashicorpVaultService = hashicorpVaultService;
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
        hashicorpVaultService.StoreJwtPublicKeyInVault(publicKey);
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
    public TokenResult GenerateJwtTokenAndStatefulRefreshToken(UtilisateurDto utilisateurDto)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var additionalAudiences = new[] { configuration.GetSection("JwtSettings")["Audiences"] };
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
            Claims = new Dictionary<string, object> { { JwtRegisteredClaimNames.Aud, additionalAudiences } }
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
        var Parameter = await redisCache.GetBooleanAndUserDataFromRedisUsingParamsAsync(tupleParameter.IsValid, tupleParameter.email!, tupleParameter.password!);
        log.LogInformation("User details have been correctly retrieved from Redis cache db");
        return Parameter.Item2;
    }
}