using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Authentifications.Interfaces;
using Authentifications.Models;
using System.Security.Claims;
using Authentifications.Controllers;
using Microsoft.AspNetCore.Session;
using Microsoft.AspNetCore.Http.Features;

namespace Authentifications.Tests;
public class UnitTestTokenController
{
    private readonly Mock<IJwtAccessAndRefreshTokenService> _mockJwtService;
    private readonly TokenController _controller;

    public UnitTestTokenController()
    {
        _mockJwtService = new Mock<IJwtAccessAndRefreshTokenService>();
        _controller = new TokenController(_mockJwtService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task Authentificate_ReturnsBadRequest_WhenEmailOrPasswordIsMissing()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await _controller.Authentificate();

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
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };

        // Act
        var result = await _controller.Authentificate();

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
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.Name, "test") }, "mock"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };

        _mockJwtService.Setup(service => service.AuthUserDetailsAsync(It.IsAny<(bool, string, string)>()))
            .ReturnsAsync(new UtilisateurDto());
        _mockJwtService.Setup(service => service.GetToken(It.IsAny<UtilisateurDto>()))
            .Returns(new TokenResult { Response = false, Message = "Token generation failed" });

        // Act
        var result = await _controller.Authentificate();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        if (unauthorizedResult.Value is TokenResult tokenResult)
        {
            Assert.False(tokenResult.Response);
            Assert.Equal("Token generation failed", tokenResult.Message);
        }
        else
        {
            Assert.True(false, "Expected TokenResult but got null or different type.");
        }
    }
    // [Fact]
    // public async Task RegenerateAccessTokenUsingRefreshToken_ShouldReturnBadRequest_WhenRefreshTokenIsMissing()
    // {
    //     // Arrange
    //     var context = new DefaultHttpContext();
    //     var sessionMock = new Mock<ISession>();
    //     var sessionFeature = new SessionFeature
    //     {
    //         Session = sessionMock.Object
    //     };
    //     context.Features.Set<ISessionFeature>(sessionFeature);
    //     _controller.ControllerContext = new ControllerContext
    //     {
    //         HttpContext = context
    //     };

    //     // Act
    //     var result = await _controller.RegenerateAccessTokenUsingRefreshToken(null);

    //     // Assert
    //     var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
    //     if (badRequestResult.Value is ProblemDetails badRequestValue)
    //     {
    //         Assert.Equal("Refresh token is required.", badRequestValue.Detail);
    //     }
    //     else
    //     {
    //         Assert.IsType<ProblemDetails>(badRequestResult.Value);
    //     }
    // }
}