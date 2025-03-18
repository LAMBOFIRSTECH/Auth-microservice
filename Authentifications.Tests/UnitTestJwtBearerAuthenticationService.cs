using Authentifications.Core.Interfaces;
using Authentifications.Core.Entities;
using Authentifications.Infrastructure.InternalServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Authentifications.Tests
{
    public class JwtAccessAndRefreshTokenServiceTest
    {
        // private readonly Mock<IConfiguration> mockConfiguration;
        // private readonly Mock<ILogger<JwtAccessAndRefreshTokenService>> mockLogger;
        // private readonly Mock<IRedisCacheService> mockRedisCache;
        // private readonly Mock<IRedisCacheTokenService> mockRedisTokenCache;
        // private readonly JwtAccessAndRefreshTokenService service;

        // public JwtAccessAndRefreshTokenServiceTest()
        // {
        //     mockConfiguration = new Mock<IConfiguration>();
        //     mockLogger = new Mock<ILogger<JwtAccessAndRefreshTokenService>>();
        //     mockRedisCache = new Mock<IRedisCacheService>();
        //     mockRedisTokenCache = new Mock<IRedisCacheTokenService>();

        //     service = new JwtAccessAndRefreshTokenService(
        //         mockConfiguration.Object,
        //         mockLogger.Object,
        //         mockRedisCache.Object,
        //         mockRedisTokenCache.Object
        //     );
        // }

        // [Fact]
        // public void GenerateRefreshToken_ShouldReturnValidToken()
        // {
        //     var refreshToken = service.GenerateRefreshToken();
        //     Assert.False(string.IsNullOrEmpty(refreshToken));
        // }

        // [Fact]
        // public async Task NewAccessTokenUsingRefreshTokenInRedisAsync_ShouldReturnTokenResult()
        // {
        //     var email = "test@example.com";
        //     var password = "password";
        //     var refreshToken = "valid_refresh_token";

        //     var utilisateurDto = new UtilisateurDto { Email = email, Pass = password };
        //     mockRedisTokenCache.Setup(x => x.RetrieveTokenBasingOnRedisUserSessionAsync(email, password))
        //         .ReturnsAsync(refreshToken);
        //     mockRedisCache.Setup(x => x.GetBooleanAndUserDataFromRedisUsingParamsAsync(true, email, password))
        //         .ReturnsAsync((true, utilisateurDto));

        //     var result = await service.NewAccessTokenUsingRefreshTokenInRedisAsync(refreshToken, email, password);

        //     Assert.NotNull(result);
        //     Assert.True(result.Response);
        // }

        // [Fact]
        // public void GetToken_ShouldReturnTokenResult()
        // {
        //     var utilisateurDto = new UtilisateurDto { Nom = "Test User", Email = "test@example.com", Role = UtilisateurDto.Privilege.Utilisateur };

        //     var result = service.GetToken(utilisateurDto);

        //     Assert.NotNull(result);
        //     Assert.True(result.Response);
        //     Assert.False(string.IsNullOrEmpty(result.Token));
        //     Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        // }

        // [Fact]
        // public void ConvertToPem_ShouldReturnValidPemFormat()
        // {
        //     var keyBytes = new byte[256];
        //     var keyType = "RSA PRIVATE KEY";

        //     var pem = JwtAccessAndRefreshTokenService.ConvertToPem(keyBytes, keyType);

        //     Assert.Contains($"-----BEGIN {keyType}-----", pem);
        //     Assert.Contains($"-----END {keyType}-----", pem);
        // }

        // [Fact]
        // public async Task AuthUserDetailsAsync_ShouldReturnUtilisateurDto()
        // {
        //     var email = "test@example.com";
        //     var password = "password";
        //     var utilisateurDto = new UtilisateurDto { Email = email, Pass = password };

        //     mockRedisCache.Setup(x => x.GetBooleanAndUserDataFromRedisUsingParamsAsync(true, email, password))
        //         .ReturnsAsync((true, utilisateurDto));

        //     var result = await service.AuthUserDetailsAsync((true, email, password));

        //     Assert.NotNull(result);
        //     Assert.Equal(email, result.Email);
        //     Assert.Equal(password, result.Pass);
        // }
    }
}