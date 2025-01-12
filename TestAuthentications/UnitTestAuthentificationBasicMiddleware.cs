using System.Text.Encodings.Web;
using Authentifications.Interfaces;
using Authentifications.Middlewares;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using System.Text;
namespace TestAuthentications;
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

    [Fact]
    public async Task HandleAuthenticateAsync_ShouldFail_WhenAuthorizationHeaderIsMissing()
    {
        // Arrange
        _httpContext.Request.Headers.Remove("Authorization");

        // Act
        var result = await _middleware.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("Authorization header missing", result.Failure?.Message);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ShouldFail_WhenAuthorizationHeaderIsInvalid()
    {
        // Arrange
        _httpContext.Request.Headers["Authorization"] = "InvalidHeader";

        // Act
        var result = await _middleware.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("Invalid Authorization header format", result.Failure?.Message);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ShouldFail_WhenEmailIsInvalid()
    {
        // Arrange
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("invalidemail:password"));
        _httpContext.Request.Headers["Authorization"] = $"Basic {credentials}";

        // Act
        var result = await _middleware.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("invalid", result.Failure?.Message);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ShouldFail_WhenCredentialsAreInvalid()
    {
        // Arrange
        var email = "user@example.com";
        var password = "wrongpassword";
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{email}:{password}"));
        _httpContext.Request.Headers["Authorization"] = $"Basic {credentials}";

        _mockRedisCache
            .Setup(r => r.GetBooleanAndUserDataFromRedisUsingParamsAsync(true, email, password))
            .ReturnsAsync((false, null));

        // Act
        var result = await _middleware.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(401, _httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ShouldSucceed_WhenCredentialsAreValid()
    {
        // Arrange
        var email = "user@example.com";
        var password = "correctpassword";
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{email}:{password}"));
        _httpContext.Request.Headers["Authorization"] = $"Basic {credentials}";

        _mockRedisCache
            .Setup(r => r.GetBooleanAndUserDataFromRedisUsingParamsAsync(true, email, password))
            .ReturnsAsync((true, null));

        // Act
        var result = await _middleware.AuthenticateAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
        Assert.Contains(result.Principal.Claims, c => c.Type == ClaimTypes.Email && c.Value == email);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ShouldFail_WhenAuthorizationHeaderEncodingIsInvalid()
    {
        // Arrange
        _httpContext.Request.Headers["Authorization"] = "Basic InvalidBase64";

        // Act
        var result = await _middleware.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("Invalid Authorization header encoding", result.Failure?.Message);
    }
}
//https://learn.microsoft.com/en-us/aspnet/core/test/middleware?view=aspnetcore-9.0


// [Fact]
// public async Task MiddlewareTest_ReturnsNotFoundForRequest()
// {
//     using var host = await new HostBuilder()
//         .ConfigureWebHost(webBuilder =>
//         {
//             webBuilder
//                 .UseTestServer()
//                 .ConfigureServices(services =>
//                 {
//                     services.AddMyServices();
//                 })
//                 .Configure(app =>
//                 {
//                     app.UseMiddleware<MyMiddleware>();
//                 });
//         })
//         .StartAsync();

//     var response = await host.GetTestClient().GetAsync("/");

// }
