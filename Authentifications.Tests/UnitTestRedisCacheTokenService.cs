using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Authentifications.Infrastructure.RedisContext;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Text;

namespace Authentifications.Tests;
public class UnitTestRedisCacheTokenService
{
    private readonly RedisCacheTokenService _service;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ILogger<RedisCacheTokenService>> _loggerMock;
    public UnitTestRedisCacheTokenService()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<RedisCacheTokenService>>();
        _service = new RedisCacheTokenService(_cacheMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void ComputeHashUsingByte_ShouldReturnHash()
    {
        const string email = "test@example.com";
        const string password = "password";
        var result = RedisCacheTokenService.ComputeHashUsingByte(email, password);
        Assert.NotNull(result);
        Assert.Equal(32, result.Length);
    }

    [Fact]
    public async Task RetrieveTokenBasingOnRedisUserSessionAsync_ShouldReturnEmptyIfNoTokenFound()
    {
        const string email = "test@example.com";
        const string password = "password123";
        string cacheKey = $"Token-{Regex.Match(email, "^[^@]+")}_{BitConverter.ToString(RedisCacheTokenService.ComputeHashUsingByte(email, password)).Replace("-", "")}";
        _cacheMock.Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>())).ReturnsAsync((byte[])null);
        var result = await _service.RetrieveTokenBasingOnRedisUserSessionAsync(email, password);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task RetrieveTokenBasingOnRedisUserSessionAsync_ShouldReturnEmptyString_WhenEmailOrPasswordIsEmpty()
    {
        var result = await _service.RetrieveTokenBasingOnRedisUserSessionAsync("", "password");
        Assert.Equal(string.Empty, result);
    }
    [Fact]
    public async Task RetrieveTokenBasingOnRedisUserSessionAsync_ShouldReturnEmptyString_WhenTokenDoesNotExistInCache()
    {
        const string email = "test@example.com";
        const string password = "password";
        string cacheKey = $"Token-test_{BitConverter.ToString(RedisCacheTokenService.ComputeHashUsingByte(email, password)).Replace("-", "")}";
        _cacheMock.Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>())).ReturnsAsync((byte[])null);
        var result = await _service.RetrieveTokenBasingOnRedisUserSessionAsync(email, password);
        Assert.Equal(string.Empty, result);
    }
    [Fact]
    public async Task RetrieveTokenBasingOnRedisUserSessionAsync_ShouldReturnToken_WhenTokenExistsInCache()
    {
        const string email = "test@example.com";
        const string password = "password";
        string cacheKey = $"Token-test_{BitConverter.ToString(RedisCacheTokenService.ComputeHashUsingByte(email, password)).Replace("-", "")}";
        var cachedData = JsonConvert.SerializeObject(new Dictionary<string, object> { { "refreshToken", "testToken" } });
        _cacheMock.Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>())).ReturnsAsync(Encoding.UTF8.GetBytes(cachedData));
        var result = await _service.RetrieveTokenBasingOnRedisUserSessionAsync(email, password);
        Assert.Equal("testToken", result);
    }

    [Fact]
    public void StoreRefreshTokenSessionInRedis_ShouldStoreTokenInCache()
    {
        const string email = "test@example.com";
        const string password = "password";
        const string refreshToken = "refreshToken";
        string cacheKey = $"Token-test_{Convert.ToHexString(RedisCacheTokenService.ComputeHashUsingByte(email, password))}";
        _cacheMock.Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>())).ReturnsAsync((byte[])null);
        _service.StoreRefreshTokenSessionInRedis(email, refreshToken, password);
        _cacheMock.Verify(x => x.SetAsync(
            cacheKey,
            It.IsAny<byte[]>(),
            It.Is<DistributedCacheEntryOptions>(options => options != null),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}