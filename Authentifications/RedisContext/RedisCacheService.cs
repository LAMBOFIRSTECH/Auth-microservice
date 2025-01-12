using System.Security.Cryptography;
using System.Text;
using Authentifications.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;
using System.Net.Sockets;
using Authentifications.Interfaces;
namespace Authentifications.RedisContext;
public class RedisCacheService : IRedisCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> logger;
    private readonly IConfiguration configuration;
    private readonly HttpClient httpClient = null!;
    private readonly string baseUrl = string.Empty;
    private readonly string cacheKey = string.Empty;
    private static DateTime _lastExecution = DateTime.MinValue;

    public RedisCacheService(IConfiguration configuration, IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        this.configuration = configuration;
        this.logger = logger;
        baseUrl = configuration["ApiSettings:BaseUrl"];
        httpClient = CreateHttpClient(baseUrl);
        cacheKey = $"ExternalDataApi_{GenerateRedisKeyForExternalDataApi()}";
    }
    public HttpClient CreateHttpClient(string baseUrl)
    {
        try
        {
            var certificateFile = configuration["Certificate:File"];
            var certificatePassword = configuration["Certificate:Password"];
            var certificate = new X509Certificate2(certificateFile, certificatePassword);
            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(certificate);
            handler.ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, certChain, sslPolicyErrors) =>
            {
                if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                {
                    return true;
                }
                logger.LogError("SSL error detected : {SslErrors}", sslPolicyErrors);
                return false;
            };
            return new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during the HttpClient creation");
            throw;
        }
    }
    public string GenerateRedisKeyForExternalDataApi()
    {
        string salt = "RandomUniqueSalt";
        string email = "example@example.com";
        string password = "password$1";
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
            var utilisateurs = await RetrieveDataOnRedisUsingKeyAsync();
            foreach (var user in utilisateurs)
            {
                var checkHashPass = user.CheckHashPassword(password);
                if (checkHashPass.Equals(true) && user.Email!.Equals(email))
                {
                    return (true, user);
                }
            }
        }
        return (false, null!);
    }
    public async Task BackGroundJob()
    {
        if ((DateTime.Now - _lastExecution).TotalMinutes >= 2)
        {
            _lastExecution = DateTime.Now;
            await RetrieveDataOnRedisUsingKeyAsync();
        }
    }
    public void DeleteRedisCacheAfterOneDay()
    {
        if ((DateTime.Now - _lastExecution).TotalMinutes >= 5)
        {
            _lastExecution = DateTime.Now;
            _cache.Remove(cacheKey);
            logger.LogInformation("Deleting sucessfully.");
        }
    }
    public async Task<ICollection<UtilisateurDto>> RetrieveDataFromExternalApiAsync()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/lambo-tasks-management/api/v1/users");
            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<HashSet<UtilisateurDto>>(content)!;
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException socketEx)
        {
            logger.LogError("Socket's problems check if TasksManagement service is UP: {socketEx.Message}", socketEx.Message);
            throw new Exception("The service is unavailable. Please retry soon.", ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while calling the API.");
            throw;
        }
    }
    public async Task<ICollection<UtilisateurDto>> RetrieveDataOnRedisUsingKeyAsync()
    {
        var cachedData = await _cache.GetStringAsync(cacheKey);
        if (cachedData is not null)
        {
            try
            {
                var result = await ValidateAndSyncDataAsync(cachedData);
                if (result is not null)
                {
                    return result;
                }
                return JsonConvert.DeserializeObject<HashSet<UtilisateurDto>>(cachedData)!;
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to Validate data between Redis and External API Service. Error : {Message}", ex.Message);
                logger.LogWarning("Using Redis cache data for requirements.");
                return JsonConvert.DeserializeObject<HashSet<UtilisateurDto>>(cachedData)!;
            }
        }
        logger.LogInformation("No data to retrieve in Redis cache.");
        var utilisateurs = await RetrieveDataFromExternalApiAsync();
        if (utilisateurs == null || !utilisateurs.Any())
        {
            throw new Exception("Failed to deserialize the response. Empty data retrieve from data source");
        }
        await UpdateRedisCacheWithExternalApiData(utilisateurs);
        return utilisateurs;
    }
    public async Task<ICollection<UtilisateurDto>> ValidateAndSyncDataAsync(string cachedData)
    {
        try
        {
            var externalApiData = (await RetrieveDataFromExternalApiAsync()).ToHashSet();
            if (externalApiData == null || !externalApiData.Any())
            {
                logger.LogWarning("Empty data return from external API. No Validation possible.");
                return null!;
            }
            var redisData = JsonConvert.DeserializeObject<HashSet<UtilisateurDto>>(cachedData)!;
            if (!redisData!.SetEquals(externalApiData))
            {
                logger.LogInformation("Loading data synchronization ...");
                await UpdateRedisCacheWithExternalApiData(externalApiData);
                return externalApiData;
            }
            logger.LogInformation("Successfull data synchronization between Redis and external.");
            return redisData;
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException)
        {
            logger.LogWarning("Impossible to valide data with external API. Unreachesable API.");
            return null!;
        }
        catch (Exception ex)
        {
            logger.LogError("Error : {Message}", ex.Message);
            throw;
        }
    }
    public async Task UpdateRedisCacheWithExternalApiData(ICollection<UtilisateurDto> externalApiData)
    {
        var serializedData = JsonConvert.SerializeObject(externalApiData);
        await _cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60)
        });
        logger.LogInformation("Redis cache data updated for redis cache key : {CacheKey}", cacheKey);
    }
}