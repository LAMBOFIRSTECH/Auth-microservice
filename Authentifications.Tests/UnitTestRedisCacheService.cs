using System;
using System.Text;
using Authentifications.Models;
using Authentifications.RedisContext;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;
using Moq;
using Moq.Protected;
using System.Collections.Generic;
using System.Threading;
using System.Security.Cryptography;

namespace Authentifications.Tests;
public class UnitTestRedisCacheService
{
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly Mock<ILogger<RedisCacheService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<RedisCacheService> _mockService; // Mock RedisCacheService

    public UnitTestRedisCacheService()
    {
        _mockCache = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<RedisCacheService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["ApiSettings:BaseUrl"]).Returns("http://localhost");

        // Créez un mock de RedisCacheService pour simuler CreateHttpClient
        _mockService = new Mock<RedisCacheService>(_mockConfiguration.Object, _mockCache.Object, _mockLogger.Object);
        _mockService.Protected()
                    .Setup<HttpClient>("CreateHttpClient", ItExpr.IsAny<string>())
                    .Returns(new HttpClient()); // Retourner un HttpClient "normal", sans certificat
    }

    // [Fact]
    // public async Task RetrieveDataFromExternalApiAsync_ShouldReturnDataWhenSuccess()
    // {
    //     // Arrange
    //     var utilisateurList = new HashSet<UtilisateurDto>
    //     {
    //         new UtilisateurDto { Email = "example@example.com" }
    //     };

    //   _mockCache.Setup(c => c.GetStringAsync(It.Is<string>(x => x == "some_specific_key")))
    //       .ReturnsAsync(JsonConvert.SerializeObject(utilisateurList));
    //     // Act
    //     var result = await _mockService.Object.RetrieveDataFromExternalApiAsync();

    //     // Assert
    //     Assert.NotNull(result);
    //     Assert.NotEmpty(result);
    //     Assert.Equal("example@example.com", result.First().Email);
    // }
}
