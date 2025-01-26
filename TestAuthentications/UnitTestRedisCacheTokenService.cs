using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Authentifications.RedisContext;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Text;
using System.Security.Cryptography;

namespace TestAuthentications;

public class RedisCacheTokenService
{
    private readonly IConfiguration _configuration;
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheTokenService(IConfiguration configuration, IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _configuration = configuration;
        _cache = cache;
        _logger = logger;
    }

    public static byte[] ComputeHashUsingByte(string email, string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var combinedBytes = Encoding.UTF8.GetBytes(email + password);
            return sha256.ComputeHash(combinedBytes);
        }
    }

    public async Task<string> RetrieveTokenBasingOnRedisUserSessionAsync(string email, string password)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            return string.Empty;
        }

        string cacheKey = $"Token-{Regex.Match(email, "^[^@]+")}_{BitConverter.ToString(ComputeHashUsingByte(email, password)).Replace("-", "")}";
        var cachedData = await _cache.GetAsync(cacheKey);

        if (cachedData == null)
        {
            return string.Empty;
        }

        var cachedString = Encoding.UTF8.GetString(cachedData);
        var tokenData = JsonConvert.DeserializeObject<Dictionary<string, object>>(cachedString);

        if (tokenData != null && tokenData.TryGetValue("refreshToken", out var refreshToken))
        {
            return refreshToken.ToString();
        }

        return string.Empty;
    }

    public void StoreRefreshTokenSessionInRedis(string email, string refreshToken, string password)
    {
        string cacheKey = $"Token-{Regex.Match(email, "^[^@]+")}_{BitConverter.ToString(ComputeHashUsingByte(email, password)).Replace("-", "")}";
        var tokenData = new Dictionary<string, object> { { "refreshToken", refreshToken } };
        var cachedData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(tokenData));

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };

        _cache.SetAsync(cacheKey, cachedData, options);
    }
}
public class UnitTestRedisCacheTokenService
{
    private readonly RedisCacheTokenService _service;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ILogger<RedisCacheService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    public UnitTestRedisCacheTokenService()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<RedisCacheService>>();
        _configurationMock = new Mock<IConfiguration>();
        _service = new RedisCacheTokenService(_configurationMock.Object, _cacheMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void ComputeHashUsingByte_ShouldReturnHash()
    {
        // Arrange
        string email = "test@example.com";
        string password = "password";

        // Act
        var result = RedisCacheTokenService.ComputeHashUsingByte(email, password);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(32, result.Length);
    }

    [Fact]
    public async Task RetrieveTokenBasingOnRedisUserSessionAsync_ShouldReturnEmptyIfNoTokenFound()
    {
        // Arrange
        string email = "test@example.com";
        string password = "password123";
        string cacheKey = $"Token-{Regex.Match(email, "^[^@]+")}_{BitConverter.ToString(RedisCacheTokenService.ComputeHashUsingByte(email, password)).Replace("-", "")}";

        // Simulation de la réponse null dans le cache

        _cacheMock.Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>())).ReturnsAsync((byte[])null);
        var result = await _service.RetrieveTokenBasingOnRedisUserSessionAsync(email, password);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task RetrieveTokenBasingOnRedisUserSessionAsync_ShouldReturnEmptyString_WhenEmailOrPasswordIsEmpty()
    {
        // Act
        var result = await _service.RetrieveTokenBasingOnRedisUserSessionAsync("", "password");

        // Assert
        Assert.Equal(string.Empty, result);
    }
    [Fact]
    public async Task RetrieveTokenBasingOnRedisUserSessionAsync_ShouldReturnEmptyString_WhenTokenDoesNotExistInCache()
    {
        // Arrange
        string email = "test@example.com";
        string password = "password";
        string cacheKey = $"Token-test_{BitConverter.ToString(RedisCacheTokenService.ComputeHashUsingByte(email, password)).Replace("-", "")}";

        _cacheMock.Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>())).ReturnsAsync((byte[])null);

        // Act
        var result = await _service.RetrieveTokenBasingOnRedisUserSessionAsync(email, password);

        // Assert
        Assert.Equal(string.Empty, result);
    }
    [Fact]
    public async Task RetrieveTokenBasingOnRedisUserSessionAsync_ShouldReturnToken_WhenTokenExistsInCache()
    {
        // Arrange
        string email = "test@example.com";
        string password = "password";
        string cacheKey = $"Token-test_{BitConverter.ToString(RedisCacheTokenService.ComputeHashUsingByte(email, password)).Replace("-", "")}";

        // Mock the GetStringAsync method of IDistributedCache directly without using the extension
        var cachedData = JsonConvert.SerializeObject(new Dictionary<string, object> { { "refreshToken", "testToken" } });

        _cacheMock.Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>())).ReturnsAsync(Encoding.UTF8.GetBytes(cachedData));

        // Act
        var result = await _service.RetrieveTokenBasingOnRedisUserSessionAsync(email, password);

        // Assert
        Assert.Equal("testToken", result);
    }



    [Fact]
    public void StoreRefreshTokenSessionInRedis_ShouldStoreTokenInCache()
    {
        // Arrange
        string email = "test@example.com";
        string password = "password";
        string refreshToken = "refreshToken";
        string cacheKey = $"Token-test_{Convert.ToHexString(RedisCacheTokenService.ComputeHashUsingByte(email, password))}";

        // Simulate that GetAsync returns null when the cache key is requested
        _cacheMock.Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>())).ReturnsAsync((byte[])null);

        // Act
        _service.StoreRefreshTokenSessionInRedis(email, refreshToken, password);

        // Assert
        // Verify that SetAsync was called once with the correct parameters
        _cacheMock.Verify(x => x.SetAsync(
            cacheKey,
            It.IsAny<byte[]>(),
            It.Is<DistributedCacheEntryOptions>(options => options != null),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}