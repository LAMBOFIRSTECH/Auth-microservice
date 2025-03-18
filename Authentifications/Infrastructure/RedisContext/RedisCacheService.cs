using System.Security.Cryptography;
using System.Text;
using Authentifications.Application.DTOs;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Authentifications.Core.Interfaces;
using Hangfire;
using Microsoft.Extensions.Caching.Memory;
namespace Authentifications.Infrastructure.RedisContext;
public class RedisCacheService : IRedisCacheService
{
    private readonly IDistributedCache _cache;
    private readonly IMemoryCache cacheMemory;
    private readonly ILogger<RedisCacheService> logger;
    private readonly string cacheKey;
    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger, IMemoryCache cacheMemory)
    {
        _cache = cache;
        this.logger = logger;
        cacheKey = $"ExternalData_{GenerateRedisKeyForExternalData()}";
        this.cacheMemory = cacheMemory;
    }
    public static string GenerateRedisKeyForExternalData()
    {
        const string salt = "RandomUniqueSalt";
        const string email = "example@example.com";
        const string password = "password$1";
        using SHA256 sha256 = SHA256.Create();
        string combined = $"{email}:{password}:{salt}";
        byte[] bytes = Encoding.UTF8.GetBytes(combined);
        byte[] hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes);
    }
     public async Task<(bool, UtilisateurDto)> GetBooleanAndUserDataFromRedisUsingParamsAsync(bool condition, string email, string password)
    {
        if (condition)
        {
            if (!cacheMemory.TryGetValue("hashpass", out string? hashpass) || string.IsNullOrWhiteSpace(hashpass))
                throw new InvalidOperationException("Temps de mise en cache du mot de passe hashé expiré");
            var utilisateurs = await GetDataOnRedisUsingKeyAsync();
            foreach (var user in utilisateurs)
            {
                var checkHashPass = user.CheckHashPassword(password, hashpass);
                if (checkHashPass && user.Email!.Equals(email))
                    return (true, user);
            }
        }
        return (false, null!);
    }
    public async Task<ICollection<UtilisateurDto>> GetUserUsingUuidAsync(ICollection<UtilisateurDto> utilisateurs)
    {
        if (!cacheMemory.TryGetValue("message", out string? message) || string.IsNullOrWhiteSpace(message))
            return null!;
        string operation = message.Split('|')[0];
        if (operation.Contains("User created"))
            return await HangFireInsertInRedisCacheOpenLdapUserDataAsync(utilisateurs);

        if (operation.Contains("User modified"))
            BackgroundJob.Enqueue(() => HangFireUpdateRedisCacheUserWithOpenLdapDataAsync(utilisateurs));

        if (operation.Contains("User deleted"))
            BackgroundJob.Enqueue(() => HangFireDeleteRedisUserDataAsync(utilisateurs));
        return null!;
    }
    public async Task<HashSet<UtilisateurDto>> GetDataOnRedisUsingKeyAsync()
    {
        string? cachedData;
        try
        {
            cachedData = await _cache.GetStringAsync(cacheKey);
            if (string.IsNullOrEmpty(cachedData))
            {
                logger.LogWarning("💢 No cache data found for key: {CacheKey}", cacheKey);
                return null!;
            }
            return JsonConvert.DeserializeObject<HashSet<UtilisateurDto>>(cachedData) ?? new HashSet<UtilisateurDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Failed to retrieve cache data for key: {CacheKey}", cacheKey);
            return null!;
        }
    }
    public async Task FindExistingUserInRedisCache(ICollection<UtilisateurDto> commingUsers)
    {
        var redisExistingUsers = await GetDataOnRedisUsingKeyAsync();
        var a = from user in redisExistingUsers select user.ID;
        var redis_user_guuid = a.AsQueryable().FirstOrDefault();
        if (commingUsers == null || commingUsers.Count == 0)
        {
            logger.LogWarning("💢 No users provided for operationcls.");
            return;
        }
        var openldapUserId = commingUsers.FirstOrDefault()?.ID;
        if (openldapUserId == null)
        {
            logger.LogWarning("💢 Invalid user ID provided.");
            return;
        }
        if (redis_user_guuid != openldapUserId)
        {
            logger.LogWarning("💢 The providing user doesn't exist in context.");
            return;
        }
        // on retourne les id redisExistingUsers
    }
    public async Task MakeUserOperationInRedisCache(ICollection<UtilisateurDto> utilisateurs)
    {
        try
        {
            var serializedData = JsonConvert.SerializeObject(utilisateurs);
            await _cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
            });

            logger.LogInformation("✅ User operation done successfully from Redis cache {CacheKey}.", cacheKey);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ User operation fail in Redis cache.");
        }
    }
    public async Task<ICollection<UtilisateurDto>> HangFireInsertInRedisCacheOpenLdapUserDataAsync(ICollection<UtilisateurDto> utilisateurs)
    {
        var cachedData = await _cache.GetStringAsync(cacheKey);
        var redisExistingUsers = new HashSet<UtilisateurDto>();
        if (cachedData is not null)
        {
            redisExistingUsers = JsonConvert.DeserializeObject<HashSet<UtilisateurDto>>(cachedData) ?? new HashSet<UtilisateurDto>();
            var a = from user in redisExistingUsers select user.ID;
            var openldap_user_guuid = utilisateurs.Select(user => user.ID).AsQueryable().FirstOrDefault();
            var redis_user_guuid = a.AsQueryable().FirstOrDefault();
            var newUsers = utilisateurs.Where(_ => !a.Contains(openldap_user_guuid)).ToList();
            if (redis_user_guuid == openldap_user_guuid)
            {
                logger.LogWarning("🎯 The providing user already exist in context.");
                return null!;
            }
            if (newUsers?.Any() != true)
            {
                logger.LogWarning("💢 Failed to deserialize the response. Empty data retrieved from data source");
                return null!;
            }
            redisExistingUsers.UnionWith(newUsers);
        }
        else
        {
            logger.LogWarning("💢 Nothing found in redis cache add the new user.");
            redisExistingUsers.UnionWith(utilisateurs);
        }
        var updatedData = JsonConvert.SerializeObject(redisExistingUsers);
        await _cache.SetStringAsync(cacheKey, updatedData, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
        });
        logger.LogInformation("✅ A new user has been stored in Redis cache.");
        return utilisateurs;
    }
    public async Task HangFireUpdateRedisCacheUserWithOpenLdapDataAsync(ICollection<UtilisateurDto> commingUsers)
    {
        await FindExistingUserInRedisCache(commingUsers);
        await MakeUserOperationInRedisCache(commingUsers);
    }
    public async Task HangFireDeleteRedisUserDataAsync(ICollection<UtilisateurDto> commingUsers)
    {
        var redisExistingUsers = await GetDataOnRedisUsingKeyAsync();
        if (commingUsers == null || commingUsers.Count == 0)
        {
            logger.LogWarning("No users provided for deletion.");
            return;
        }
        var openldapUserId = commingUsers.FirstOrDefault()?.ID;
        if (openldapUserId == null)
        {
            logger.LogWarning("Invalid user ID provided.");
            return;
        }
        int removedCount = redisExistingUsers.RemoveWhere(user => user.ID == openldapUserId);
        if (removedCount == 0)
        {
            logger.LogWarning("User with ID {UserId} not found in Redis cache.", openldapUserId);
            return;
        }
        await MakeUserOperationInRedisCache(redisExistingUsers);
    }
}