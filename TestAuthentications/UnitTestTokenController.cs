using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Authentifications.Controllers;
using Authentifications.Interfaces;
using Authentifications.Models;
using System.Text;
namespace TestAuthentications;
public class UnitTestTokenController
{
    private readonly Mock<IJwtAccessAndRefreshTokenService> mockJwtTokenService;
    private readonly Mock<IRedisCacheTokenService> mockRedisTokenCacheService;
    private readonly Mock<IRedisCacheService> mockRedisCacheService;
    private readonly Mock<ILogger<TokenController>> mockLogger;
    private readonly TokenController controller;

    public UnitTestTokenController()
    {
        mockJwtTokenService = new Mock<IJwtAccessAndRefreshTokenService>();
        mockRedisTokenCacheService = new Mock<IRedisCacheTokenService>();
        mockRedisCacheService = new Mock<IRedisCacheService>();
        mockLogger = new Mock<ILogger<TokenController>>();
        controller = new TokenController(mockLogger.Object, mockRedisTokenCacheService.Object, mockJwtTokenService.Object, mockRedisCacheService.Object);
    }

    [Fact]
    public async Task Authentificate_ReturnsBadRequest_WhenEmailOrPasswordIsMissing()
    {
        // Arrange
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await controller.Authentificate();

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Email or password is missing.", badRequestResult.Value);
    }

    [Fact]
    public async Task Authentificate_ReturnsUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Items["email"] = "test@example.com";
        context.Items["password"] = "password";
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };

        // Act
        var result = await controller.Authentificate();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Unauthorized access", unauthorizedResult.Value);
    }

    [Fact]
    public async Task Authentificate_ReturnsUnauthorized_WhenTokenGenerationFails()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Items["email"] = "test@example.com";
        context.Items["password"] = "password";
        context.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity("Basic"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };

        mockJwtTokenService.Setup(service => service.AuthUserDetailsAsync(It.IsAny<(bool, string, string)>()))
            .ReturnsAsync(new UtilisateurDto());
        mockJwtTokenService.Setup(service => service.GetToken(It.IsAny<UtilisateurDto>()))
            .Returns(new TokenResult
            {
                Response = false,
                Message = "Token generation failed",
                Token = null,
                RefreshToken = null
            });

        // Act
        var result = await controller.Authentificate();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var tokenResult = Assert.IsType<TokenResult>(unauthorizedResult.Value);
        Assert.Equal("Token generation failed", tokenResult.Message);
    }


    // [Fact]
    // public async Task Authentificate_ReturnsCreatedAtAction_WhenTokenGenerationSucceeds()
    // {
    //     // Arrange
    //     var context = new DefaultHttpContext();

    //     // Appel à la méthode SetSession pour initialiser la session
    //     context.SetSession();

    //     // Ajout des données dans context.Items (pas dans la session)
    //     context.Items["email"] = "test@example.com";
    //     context.Items["password"] = "password";
    //     context.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity("Basic"));

    //     // Attacher le contexte HTTP au contrôleur
    //     controller.ControllerContext = new ControllerContext
    //     {
    //         HttpContext = context
    //     };

    //     // Création de l'objet TokenResult simulé
    //     var tokenResult = new TokenResult
    //     {
    //         Response = true,
    //         Token = "accessToken",
    //         RefreshToken = "refreshToken"
    //     };

    //     // Configuration des comportements simulés
    //     _ = mockJwtTokenService.Setup(service => service.AuthUserDetailsAsync(It.IsAny<(bool, string, string)>()))
    //         .ReturnsAsync(new UtilisateurDto());
    //     mockJwtTokenService.Setup(service => service.GetToken(It.IsAny<UtilisateurDto>()))
    //         .Returns(tokenResult);

    //     // Act
    //     var result = await controller.Authentificate();

    //     // Assert
    //     var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
    //     var returnedTokenResult = Assert.IsType<TokenResult>(createdAtActionResult.Value);
    //     Assert.True(returnedTokenResult.Response);
    //     Assert.Equal("accessToken", returnedTokenResult.Token);
    //     Assert.Equal("refreshToken", returnedTokenResult.RefreshToken);
    // }
}


