using System.Security.Cryptography;
using System.Text;
using Authentifications.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;
using System.Net.Sockets;
using Authentifications.Interfaces;
using Hangfire;
namespace Authentifications.RedisContext;
public class RedisCacheService : IRedisCacheService
{
	private readonly IDistributedCache _cache;
	private readonly ILogger<RedisCacheService> logger;
	private readonly IConfiguration configuration;
	private readonly string baseUrl;
	private readonly string cacheKey;

	public RedisCacheService(IConfiguration configuration, IDistributedCache cache, ILogger<RedisCacheService> logger)
	{
		_cache = cache;
		this.configuration = configuration;
		this.logger = logger;
		baseUrl = configuration["ApiSettings:BaseUrl"]!;
		cacheKey = $"ExternalDataApi_{GenerateRedisKeyForExternalDataApi()}";
	}
	public HttpClient CreateHttpClient(string baseUrl)
	{
		try
		{ var certificateFile = configuration["Kestrel:EndPoints:Https:Certificate:File"];
			var certificatePassword = configuration["Kestrel:EndPoints:Https:Certificate:Password"];
			if (string.IsNullOrWhiteSpace(certificateFile) || string.IsNullOrWhiteSpace(certificatePassword))
				throw new InvalidOperationException("Certificat path and password are missing");
			var certificate = new X509Certificate2(certificateFile, certificatePassword);
			var handler = new HttpClientHandler();
			handler.ClientCertificates.Add(certificate);
			handler.ServerCertificateCustomValidationCallback = (_, cert, __, sslPolicyErrors) =>
			{
				if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None || cert!.Subject.Contains("CN=localhost"))
					return true;
				if (cert != null)
				{
					logger.LogError("Certificate subject: {CertSubject}", cert.Subject);
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
			var utilisateurs = await GetDataOnRedisUsingKeyAsync();
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
	public async Task<ICollection<UtilisateurDto>> GetDataOnRedisUsingKeyAsync()
	{
		var cachedData = await _cache.GetStringAsync(cacheKey);
		if (cachedData is not null)
			return JsonConvert.DeserializeObject<HashSet<UtilisateurDto>>(cachedData)!;
		var utilisateurs = JsonConvert.DeserializeObject<HashSet<UtilisateurDto>>(cachedData!);
		if (utilisateurs?.Any() != true)
		{
			logger.LogWarning("Failed to deserialize the response. Empty data retrieved from redis cache");
			return null!;
		}
		return utilisateurs;
	}
	[Queue("store_into_redis")]
    public async Task<ICollection<UtilisateurDto>> HangFireRetrieveDataOnRedisUsingKeyAsync()
    {
        var cachedData = await _cache.GetStringAsync(cacheKey);
        if (cachedData is not null)
        {
            var result = await ValidateAndSyncRedisDataAsync(cachedData);
            return result ?? JsonConvert.DeserializeObject<HashSet<UtilisateurDto>>(cachedData)!;
        }
        logger.LogWarning("No data to retrieve in Redis cache server.");
        logger.LogInformation("Try to retrieve data from external API.");
        var utilisateurs = await RetrieveDataFromExternalApiAsync();
        if (utilisateurs?.Any() != true)
        {
            logger.LogWarning("Failed to deserialize the response. Empty data retrieved from data source");
            return null!;
        }
        await UpdateRedisCacheWithExternalApiData(utilisateurs);
        return utilisateurs;
    }
	public async Task<ICollection<UtilisateurDto>> RetrieveDataFromExternalApiAsync()
	{
		var httpClient = CreateHttpClient(baseUrl);
		try
		{
			using var request = new HttpRequestMessage(HttpMethod.Get, "/lambo-tasks-management/data/users");
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
			throw new InvalidOperationException("There was an error while calling the external API", ex);
		}
	}
	 public async Task<ICollection<UtilisateurDto>> ValidateAndSyncRedisDataAsync(string cachedData)
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
        logger.LogInformation("Successful data synchronization between Redis and external api.");
        return redisData;
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
	public async Task UpdateRedisCacheWithExternalApiData(ICollection<UtilisateurDto> externalApiData)
	{
		var serializedData = JsonConvert.SerializeObject(externalApiData);
		await _cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
		{
			AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
		});
		logger.LogInformation("Redis cache data updated for redis cache key : {CacheKey}", cacheKey);
	}
}
