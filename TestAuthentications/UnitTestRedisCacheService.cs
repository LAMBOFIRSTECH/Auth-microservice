using System.Text;
using Authentifications.Models;
using Authentifications.RedisContext;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;

namespace TestAuthentications;

public class UnitTestRedisCacheService
{
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ILogger<RedisCacheService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly RedisCacheService _redisCacheService;

    public UnitTestRedisCacheService()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<RedisCacheService>>();
        _configurationMock = new Mock<IConfiguration>();

        _configurationMock.Setup(c => c["ApiSettings:BaseUrl"]).Returns("https://localhost:7082");
        _configurationMock.Setup(c => c["Certificate:File"]).Returns("/etc/ssl/certs/TasksApi.pfx");
        _configurationMock.Setup(c => c["Certificate:Password"]).Returns("lambo");

        _redisCacheService = new RedisCacheService(_configurationMock.Object, _cacheMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void CreateHttpClient_ShouldReturnHttpClient()
    {
        // Act
        var httpClient = _redisCacheService.CreateHttpClient("https://localhost:7082");

        // Assert
        Assert.NotNull(httpClient);
        Assert.Equal("https://localhost:7082", httpClient.BaseAddress.ToString());
    }

    [Fact]
    public void GenerateRedisKeyForExternalDataApi_ShouldReturnKey()
    {
        // Act
        var key = _redisCacheService.GenerateRedisKeyForExternalDataApi();

        // Assert
        Assert.NotNull(key);
        Assert.NotEmpty(key);
    }

    [Fact]
    public async Task GetBooleanAndUserDataFromRedisUsingParamsAsync_ReturnsTrueAndUser_WhenConditionIsTrueAndUserExists()
    {
        // Arrange
        var email = "test@example.com";
        var password = "password";
        var utilisateurs = new List<UtilisateurDto>
            {
                new() { Email = email, Pass = "hashedPassword" }
            };
        _cacheMock.Setup(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((string key, CancellationToken token) => JsonConvert.SerializeObject(utilisateurs));
        var condition = true;

        // Act
        var result = await _redisCacheService.GetBooleanAndUserDataFromRedisUsingParamsAsync(condition, email, password);

        // Assert
        Assert.True(result.Item1);
        Assert.NotNull(result.Item2);
        Assert.Equal(email, result.Item2.Email);
    }

    [Fact]
    public async Task GetBooleanAndUserDataFromRedisUsingParamsAsync_ReturnsFalseAndNull_WhenConditionIsFalse()
    {
        // Arrange
        var email = "test@example.com";
        var password = "password";
        var condition = false;

        // Act
        var result = await _redisCacheService.GetBooleanAndUserDataFromRedisUsingParamsAsync(condition, email, password);

        // Assert
        Assert.False(result.Item1);
        Assert.Null(result.Item2);
    }

    [Fact]
    public async Task GetBooleanAndUserDataFromRedisUsingParamsAsync_ReturnsFalseAndNull_WhenUserDoesNotExist()
    {
        // Arrange
        var email = "test@example.com";
        var password = "password";
        var utilisateurs = new List<UtilisateurDto>();
        _cacheMock.Setup(x => x.GetStringAsync(It.IsAny<string>())).ReturnsAsync(JsonConvert.SerializeObject(utilisateurs));
        var condition = true;

        // Act
        var result = await _redisCacheService.GetBooleanAndUserDataFromRedisUsingParamsAsync(condition, email, password);

        // Assert
        Assert.False(result.Item1);
        Assert.Null(result.Item2);
    }


    [Fact]
    public async Task RetrieveDataFromExternalApiAsync_ShouldReturnData()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var mockResponse = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new StringContent("[{\"Email\":\"example@example.com\",\"Password\":\"password$1\"}]")
        };
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()
            )
            .ReturnsAsync(mockResponse);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);

        // Assuming _redisCacheService has a constructor that accepts HttpClient
        var _redisCacheService = new RedisCacheService(httpClient, _cacheMock.Object);

        // Act
        var result = await _redisCacheService.RetrieveDataFromExternalApiAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task RetrieveDataOnRedisUsingKeyAsync_ShouldReturnData()
    {
        // Arrange
        var cachedData = "[{\"Email\":\"example@example.com\",\"Password\":\"password$1\"}]";
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Encoding.UTF8.GetBytes(cachedData));

        // Act
        var result = await _redisCacheService.RetrieveDataOnRedisUsingKeyAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task UpdateRedisCacheWithExternalApiData_ShouldUpdateCache()
    {
        // Arrange
        var data = new List<UtilisateurDto> { new() { Email = "example@example.com", Pass = "password$1" } };

        // Act
        await _redisCacheService.UpdateRedisCacheWithExternalApiData(data);

        // Assert
        _cacheMock.Verify(c => c.SetAsync(
         It.IsAny<string>(),
         It.IsAny<byte[]>(),
         It.IsAny<DistributedCacheEntryOptions>(),
         It.IsAny<CancellationToken>()),
         Times.Once);

    }
}


