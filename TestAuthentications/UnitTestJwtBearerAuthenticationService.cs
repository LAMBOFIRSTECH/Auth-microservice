using Authentifications.Interfaces;
using Authentifications.Models;
using Authentifications.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Cryptography;

namespace TestAuthentications;
public class UnitTestJwtBearerAuthenticationService
{
	private readonly Mock<Microsoft.Extensions.Configuration.IConfiguration> mockConfiguration;
	private readonly Mock<ILogger<JwtAccessAndRefreshTokenService>> mockLogger;
	private readonly Mock<IRedisCacheService> mockRedisCache;
	private readonly Mock<IRedisCacheTokenService> mockRedisTokenCache;
	private readonly JwtAccessAndRefreshTokenService service;
	public UnitTestJwtBearerAuthenticationService()
	{
		mockConfiguration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
		mockLogger = new Mock<ILogger<JwtAccessAndRefreshTokenService>>();
		mockRedisCache = new Mock<IRedisCacheService>();
		mockRedisTokenCache = new Mock<IRedisCacheTokenService>();

		service = new JwtAccessAndRefreshTokenService(
			mockConfiguration.Object,
			mockLogger.Object,
			mockRedisCache.Object,
			mockRedisTokenCache.Object
		);
	}

	[Fact]
	public void GenerateRefreshToken_ShouldReturnToken()
	{
		// Act
		var refreshToken = service.GenerateRefreshToken();

		// Assert
		Assert.False(string.IsNullOrEmpty(refreshToken));
	}

	[Fact]
	public async Task NewAccessTokenUsingRefreshTokenInRedisAsync_ShouldReturnTokenResult()
	{
		// Arrange
		var email = "test@example.com";
		var password = "password";
		var refreshToken = service.GenerateRefreshToken();
		var utilisateurDto = new UtilisateurDto { Email = email, Pass = password, Nom = "Test User", Role = UtilisateurDto.Privilege.Utilisateur };

		mockRedisTokenCache.Setup(x => x.RetrieveTokenBasingOnRedisUserSessionAsync(email, password))
			.ReturnsAsync(refreshToken);
		mockRedisCache.Setup(x => x.GetBooleanAndUserDataFromRedisUsingParamsAsync(true, email, password))
			.ReturnsAsync((true, utilisateurDto));

		// Act
		var result = await service.NewAccessTokenUsingRefreshTokenInRedisAsync(refreshToken, email, password);

		// Assert
		Assert.NotNull(result);
		Assert.False(string.IsNullOrEmpty(result.Token));
		Assert.False(string.IsNullOrEmpty(result.RefreshToken));
	}

	[Fact]
	public void GetToken_ShouldReturnTokenResult()
	{
		// Arrange
		var utilisateurDto = new UtilisateurDto {ID =new Guid() ,Email = "test@example.com", Pass = "password", Nom = "Test User", Role = UtilisateurDto.Privilege.Utilisateur };

		// Act
		var result = service.GetToken(utilisateurDto);

		// Assert
		Assert.NotNull(result);
		Assert.False(string.IsNullOrEmpty(result.Token));
		Assert.False(string.IsNullOrEmpty(result.RefreshToken));
	}

	[Fact]
	public void ConvertToPem_ShouldReturnPemFormattedString()
	{
		// Arrange
		var rsa = RSA.Create(2048);
		var privateKeyBytes = rsa.ExportRSAPrivateKey();

		// Act
		var pem = JwtAccessAndRefreshTokenService.ConvertToPem(privateKeyBytes, "RSA PRIVATE KEY");

		// Assert
		Assert.False(string.IsNullOrEmpty(pem));
		Assert.Contains("-----BEGIN RSA PRIVATE KEY-----", pem);
		Assert.Contains("-----END RSA PRIVATE KEY-----", pem);
	}

	[Fact]
	public async Task AuthUserDetailsAsync_ShouldReturnUtilisateurDto()
	{
		// Arrange
		var email = "test@example.com";
		var password = "password";
		var utilisateurDto = new UtilisateurDto { Email = email, Pass = password, Nom = "Test User", Role = UtilisateurDto.Privilege.Utilisateur };

		mockRedisCache.Setup(x => x.GetBooleanAndUserDataFromRedisUsingParamsAsync(true, email, password))
			.ReturnsAsync((true, utilisateurDto));

		// Act
		var result = await service.AuthUserDetailsAsync((true, email, password));

		// Assert
		Assert.NotNull(result);
		Assert.Equal(email, result.Email);
		Assert.Equal(password, result.Pass);
	}
}
