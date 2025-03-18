using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Authentifications.Core.Interfaces;
using Authentifications.Application.DTOs;
using Microsoft.IdentityModel.Tokens;
namespace Authentifications.Infrastructure.InternalServices;
public class JwtAccessAndRefreshTokenService : IJwtAccessAndRefreshTokenService
{
    private readonly IConfiguration configuration;
    private readonly ILogger<JwtAccessAndRefreshTokenService> log;
    private readonly IRedisCacheService redisCache;
    private readonly IRedisCacheTokenService redisTokenCache;
    private readonly IHashicorpVaultService hashicorpVaultService;
    private RsaSecurityKey? rsaSecurityKey;
    private readonly string refreshToken;
    public JwtAccessAndRefreshTokenService(IConfiguration configuration, ILogger<JwtAccessAndRefreshTokenService> log,
    IRedisCacheService redisCache, IRedisCacheTokenService redisTokenCache, IHashicorpVaultService hashicorpVaultService)
    {
        this.configuration = configuration;
        this.log = log;
        this.redisCache = redisCache;
        this.redisTokenCache = redisTokenCache;
        this.hashicorpVaultService = hashicorpVaultService;
        refreshToken = GenerateRefreshToken();
    }
    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[128];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public async Task<TokenResultDto> NewAccessTokenUsingRefreshTokenInRedisAsync(string refreshToken, string email, string password)
    {
        var utilisateurDto = await AuthUserDetailsAsync((true, email, password));
        var refreshTokenFromRedis = await redisTokenCache.RetrieveTokenBasingOnRedisUserSessionAsync(utilisateurDto.Email!, utilisateurDto.Pass!);
        if (string.IsNullOrEmpty(refreshTokenFromRedis))
        {
            return new TokenResultDto
            {
                Response = false,
                Message = "💢 Empty refresh token retrieving from Redis cache",
                Token = null,
                RefreshToken = "💢 The current refresh token has expired. Please create a new token"
            };
        }
        if (!refreshTokenFromRedis.Equals(refreshToken))
        {
            return new TokenResultDto
            {
                Response = false,
                Message = "💢 The sharing refresh token is not found inside Redis cache database. Please use this generated refresh token",
                Token = null,
                RefreshToken = await redisTokenCache.RetrieveTokenBasingOnRedisUserSessionAsync(utilisateurDto.Email!, utilisateurDto.Pass!)
            };
        }
        return GetToken(utilisateurDto);
    }
    public TokenResultDto GetToken(UtilisateurDto utilisateurDto)
    {
        var tokenResult = GenerateJwtTokenAndStatefulRefreshToken(utilisateurDto);
        if (!tokenResult.Response)
        {
            return new()
            {
                Response = tokenResult.Response,
                Message = tokenResult.Message,
                Token = tokenResult.Token,
                RefreshToken = tokenResult.RefreshToken
            };
        }
        log.LogInformation("✅ Creating current user session's Token");
        redisTokenCache.StoreRefreshTokenSessionInRedis(utilisateurDto.Email!, tokenResult.RefreshToken!, utilisateurDto.Pass!);
        return tokenResult;
    }
    private RsaSecurityKey CreateRSAPublicKeyToSignJwtToken()
    {
        if (rsaSecurityKey != null)
            return rsaSecurityKey;
        var rsa = RSA.Create(2048);
        _ = ConvertToPem(rsa.ExportRSAPrivateKey(), "RSA PRIVATE KEY");
        var publicKey = ConvertToPem(rsa.ExportRSAPublicKey(), "RSA PUBLIC KEY");
        hashicorpVaultService.StoreJwtPublicKeyInVault(publicKey);
        log.LogInformation("✅ The signing jwt publicKey has been successfull store in HashiCorp Vault !");
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
    public TokenResultDto GenerateJwtTokenAndStatefulRefreshToken(UtilisateurDto utilisateurDto)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var audiencesSection = configuration.GetSection("JwtSettings:Audiences");
        var issuerSection = configuration.GetSection("JwtSettings:Issuer");
        var additionalAudiences = audiencesSection.Exists() ? audiencesSection.Get<string[]>()?.ToList() : new List<string>();
        var issuer = issuerSection.Exists() ? issuerSection.Get<string>()?.ToString() : string.Empty;
        if (additionalAudiences == null || additionalAudiences.Count == 0 || string.IsNullOrEmpty(issuer))
        {
            return new()
            {
                Response = false,
                Message = "💢 JwtSettings are missing or incorrect. Let's check the configuration file",
                Token = null,
                RefreshToken = null
            };
        }
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
            SigningCredentials = new SigningCredentials(CreateRSAPublicKeyToSignJwtToken(), SecurityAlgorithms.RsaSha512),
            Issuer = issuer,
            Claims = new Dictionary<string, object> { { JwtRegisteredClaimNames.Aud, additionalAudiences } }
        };
        var tokenCreation = tokenHandler.CreateToken(tokenDescriptor);
        var token = tokenHandler.WriteToken(tokenCreation);
        return new()
        {
            Response = true,
            Message = "🧡 AccessToken and refreshToken have been successfully generated",
            Token = token,
            RefreshToken = refreshToken
        };
    }
    public async Task<UtilisateurDto> AuthUserDetailsAsync((bool IsValid, string email, string password) tupleParameter)
    {
        var Parameter = await redisCache.GetBooleanAndUserDataFromRedisUsingParamsAsync(tupleParameter.IsValid, tupleParameter.email!, tupleParameter.password!);
        log.LogInformation("✅ User details have been correctly retrieved from Redis cache db");
        return Parameter.Item2;
    }
}