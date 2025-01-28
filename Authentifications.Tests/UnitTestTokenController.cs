using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Authentifications.Interfaces;
using Authentifications.Models;
using System.Security.Claims;

namespace Authentifications.Tests
{
    public class TokenControllerTest
    {
        private readonly Mock<IJwtAccessAndRefreshTokenService> _mockJwtService;
        private readonly TokenController _controller;

        public TokenControllerTest()
        {
            _mockJwtService = new Mock<IJwtAccessAndRefreshTokenService>();
            _controller = new TokenController(_mockJwtService.Object);
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
            var tokenResult = Assert.IsType<TokenResult>(unauthorizedResult.Value);
            Assert.False(tokenResult.Response);
            Assert.Equal("Token generation failed", tokenResult.Message);
        }

        // [Fact]
        // public async Task Authentificate_ReturnsCreatedAtAction_WhenTokenGenerationSucceeds()
        // {
        //     // Arrange
        //     var context = new DefaultHttpContext();
        //     context.Items["email"] = "test@example.com";
        //     context.Items["password"] = "password";
        //     context.User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.Name, "test") }, "mock"));
        //     _controller.ControllerContext = new ControllerContext
        //     {
        //         HttpContext = context
        //     };

        //     _mockJwtService.Setup(service => service.AuthUserDetailsAsync(It.IsAny<(bool, string, string)>()))
        //         .ReturnsAsync(new UtilisateurDto());
        //     _mockJwtService.Setup(service => service.GetToken(It.IsAny<UtilisateurDto>()))
        //         .Returns(new TokenResult { Response = true, Token = "accessToken", RefreshToken = "refreshToken" });

        //     // Act
        //     var result = await _controller.Authentificate();

        //     // Assert
        //     var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
        //     var tokenResult = Assert.IsType<TokenResult>(createdAtActionResult.Value);
        //     Assert.True(tokenResult.Response);
        //     Assert.Equal("AccessToken and refreshToken have been successfully generated ", tokenResult.Message);
        //     Assert.Equal("accessToken", tokenResult.Token);
        //     Assert.Equal("refreshToken", tokenResult.RefreshToken);
        // }
    }
}