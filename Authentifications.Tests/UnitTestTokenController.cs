using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Authentifications.Core.Interfaces;
using Authentifications.Application.DTOs;
using System.Security.Claims;
using Authentifications.Application.Controllers;

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
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        var result = await _controller.Authentificate();
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Email or password is missing.", badRequestResult.Value);
    }

    [Fact]
    public async Task Authentificate_ReturnsUnauthorized_WhenUserIsNotAuthenticated()
    {
        var context = new DefaultHttpContext();
        context.Items["email"] = "test@example.com";
        context.Items["password"] = "password";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };
        var result = await _controller.Authentificate();
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Unauthorized access 💢", unauthorizedResult.Value);
    }

    [Fact]
    public async Task Authentificate_ReturnsUnauthorized_WhenTokenGenerationFails()
    {
        var context = new DefaultHttpContext();
        context.Items["email"] = "test@example.com";
        context.Items["password"] = "password";
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(ClaimTypes.Name, "test") }, "mock"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };
        _mockJwtService.Setup(service => service.AuthUserDetailsAsync(It.IsAny<(bool, string, string)>()))
            .ReturnsAsync(new UtilisateurDto());
        _mockJwtService.Setup(service => service.GetToken(It.IsAny<UtilisateurDto>()))
            .Returns(new TokenResultDto { Response = false, Message = "Token generation failed" });
        var result = await _controller.Authentificate();
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        if (unauthorizedResult.Value is TokenResultDto tokenResult)
        {
            Assert.False(tokenResult.Response);
            Assert.Equal("Token generation failed", tokenResult.Message);
        }
        else
        {
            Assert.True(false, "Expected TokenResult but got null or different type.");
        }
    }
}