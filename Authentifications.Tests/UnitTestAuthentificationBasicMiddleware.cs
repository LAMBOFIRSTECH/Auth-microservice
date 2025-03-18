using System.Text.Encodings.Web;
using Authentifications.Core.Interfaces;
using Authentifications.Application.Middlewares;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
namespace Authentifications.Tests;
public class UnitTestAuthentificationBasicMiddleware
{
    private readonly Mock<IRedisCacheService> _mockRedisCache;
    private readonly Mock<ILogger<AuthentificationBasicMiddleware>> _mockLogger;
    private readonly AuthentificationBasicMiddleware _middleware;
    private readonly AuthenticationSchemeOptions _options;
    private readonly DefaultHttpContext _httpContext;

    public UnitTestAuthentificationBasicMiddleware()
    {
        _mockRedisCache = new Mock<IRedisCacheService>();
        _options = new AuthenticationSchemeOptions();

        var optionsMonitor = Mock.Of<IOptionsMonitor<AuthenticationSchemeOptions>>(o => o.CurrentValue == _options);
        var encoder = UrlEncoder.Default;
        var clock = Mock.Of<ISystemClock>();

        _httpContext = new DefaultHttpContext();

        var loggerFactory = Mock.Of<ILoggerFactory>();
        _middleware = new AuthentificationBasicMiddleware(
            _mockRedisCache.Object,
            optionsMonitor,
            loggerFactory,
            encoder,
            clock,
            _mockLogger.Object
        );
    }
}
