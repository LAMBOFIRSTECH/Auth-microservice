using Authentifications.Infrastructure.RedisContext;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Authentifications.Tests;
public class UnitTestRedisCacheService
{
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly Mock<ILogger<RedisCacheService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<RedisCacheService> _mockService;

    public UnitTestRedisCacheService()
    {
        _mockCache = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<RedisCacheService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["ApiSettings:BaseUrl"]).Returns("http://localhost");
        _mockService = new Mock<RedisCacheService>(_mockConfiguration.Object, _mockCache.Object, _mockLogger.Object);
        _mockService.Protected()
                    .Setup<HttpClient>("CreateHttpClient", ItExpr.IsAny<string>())
                    .Returns(new HttpClient());
    }
}
