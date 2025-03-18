using System.Security.Cryptography;
using System.Text;
using Authentifications.Core.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
namespace Authentifications.Infrastructure.RedisContext;
public class RedisCacheTokenService : IRedisCacheTokenService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheTokenService> logger;
    public RedisCacheTokenService(IDistributedCache cache, ILogger<RedisCacheTokenService> logger)
    {
        _cache = cache;
        this.logger = logger;
    }
    public static byte[] ComputeHashUsingByte(string email, string password)
    {
        const string salt = "RandomUniqueSalt";
        using SHA256 sha256 = SHA256.Create();
        string combined = $"{email}:{password}:{salt}";
        byte[] bytes = Encoding.UTF8.GetBytes(combined);
        return sha256.ComputeHash(bytes);
    }
    public async Task<string> RetrieveTokenBasingOnRedisUserSessionAsync(string email, string password)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            return string.Empty;
        var name = Regex.Match(email, "^[^@]+");
        var cacheKey = $"Token-{name}_{BitConverter.ToString(ComputeHashUsingByte(email, password)).Replace("-", "")}";
        var cachedData = await _cache.GetStringAsync(cacheKey);
        if (cachedData is null)
        {
            logger.LogWarning("No session's token existing in Redis for key: {CacheKey}", cacheKey);
            return string.Empty;
        }
        var obj = JsonConvert.DeserializeObject<Dictionary<string, object>>(cachedData)!;
        string token = "";
        if (obj.ContainsKey("refreshToken"))
            token = obj["refreshToken"]?.ToString() ?? string.Empty;
        return token;
    }
    public void StoreRefreshTokenSessionInRedis(string email, string refreshToken, string password)
    {
        Dictionary<string, object> jsonObject = new()
        {
            { "RedisRefreshTokenId", Guid.NewGuid() },
            { "Email", email },
            { "Pass", password },
            { "refreshToken", refreshToken }
        };
        var tampon = Convert.ToHexString(ComputeHashUsingByte(email, password));
        var name = Regex.Match(email, "^[^@]+");
        var cacheKey = $"Token-{name}_{tampon}";
        var cachedData = _cache.GetStringAsync(cacheKey);
        if (cachedData is not null)
        {
            var serializedData = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);
            _cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
            });
            logger.LogInformation("Successfull storage refresh token connection for key: {CacheKey}", cacheKey);
        }
    }
}