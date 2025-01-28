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
    private readonly HttpClient httpClient;
    private readonly string cacheKey;
    private static DateTime _lastExecution = DateTime.MinValue;

    public RedisCacheService(IConfiguration configuration, IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        this.configuration = configuration;
        this.logger = logger;
        string baseUrl = configuration["ApiSettings:BaseUrl"];
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
      //   if (sslPolicyErrors != System.Net.Security.SslPolicyErrors.None)
      //   {
      //       logger.LogError("SSL validation failed: {SslPolicyErrors}. Certificate: {CertSubject}", sslPolicyErrors, cert?.Subject);
      //   }
      return true; //sslPolicyErrors == System.Net.Security.SslPolicyErrors.None; il faut vérifier le certificat entre client et serveur
      //return true; A ne jamais le faire en production 
  };

            return new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during the HttpClient creation");
            throw new InvalidOperationException("Error while creating HttpClient with SSL certificate.", ex);
        }
    }
    public static string GenerateRedisKeyForExternalDataApi()
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
            var utilisateurs = await RetrieveDataOnRedisUsingKeyAsync();
            foreach (var user in utilisateurs)
            {
                var checkHashPass = user.CheckHashPassword(password);
                if (checkHashPass && user.Email!.Equals(email))
                {
                    return (true, user);
                }
            }
        }
        return (false, null!);
    }
    private static bool ShouldExecuteBackgroundTask(TimeSpan timeSpan)
    {
        return (DateTime.Now - _lastExecution).TotalMinutes >= timeSpan.TotalMinutes;
    }

    public async Task BackGroundJob()
    {
        if (ShouldExecuteBackgroundTask(TimeSpan.FromMinutes(2)))
        {
            _lastExecution = DateTime.Now;
            await RetrieveDataOnRedisUsingKeyAsync();
        }
    }

    public void DeleteRedisCacheAfterOneDay()
    {
        if (ShouldExecuteBackgroundTask(TimeSpan.FromMinutes(5)))
        {
            _lastExecution = DateTime.Now;
            _cache.Remove(cacheKey);
            logger.LogInformation("Deleting successfully.");
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
            logger.LogError(socketEx, "Socket's problems check if TasksManagement service is UP");
            throw new InvalidOperationException("The service is unavailable. Please retry soon.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while calling the API.");
            throw new InvalidOperationException("There was an error while calling the external API.", ex);
        }
    }
    public async Task<ICollection<UtilisateurDto>> RetrieveDataOnRedisUsingKeyAsync()
    {
        var cachedData = await _cache.GetStringAsync(cacheKey);
        if (cachedData is not null)
        {
            var result = await ValidateAndSyncDataAsync(cachedData);
            return result ?? JsonConvert.DeserializeObject<HashSet<UtilisateurDto>>(cachedData)!;
        }
        logger.LogInformation("No data to retrieve in Redis cache server.");
        var utilisateurs = await RetrieveDataFromExternalApiAsync();
        if (utilisateurs?.Any() != true)
        {
            logger.LogWarning("Failed to deserialize the response. Empty data retrieved from data source");
            return null!;
        }
        await UpdateRedisCacheWithExternalApiData(utilisateurs);
        return utilisateurs;
    }
    private bool IsDataEmpty(HashSet<UtilisateurDto> data)
    {
        if (data?.Any() != true)
        {
            logger.LogWarning("Empty data returned from external API. No Validation possible.");
            return true;
        }
        return false;
    }

    public async Task<ICollection<UtilisateurDto>> ValidateAndSyncDataAsync(string cachedData)
    {
        var externalApiData = (await RetrieveDataFromExternalApiAsync()).ToHashSet();
        if (IsDataEmpty(externalApiData)) return null!;
        var redisData = JsonConvert.DeserializeObject<HashSet<UtilisateurDto>>(cachedData)!;
        if (!redisData!.SetEquals(externalApiData))
        {
            logger.LogInformation("Loading data synchronization ...");
            await UpdateRedisCacheWithExternalApiData(externalApiData);
            return externalApiData;
        }
        logger.LogInformation("Successful data synchronization between Redis and external.");
        return redisData;
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