using System.Text.Encodings.Web;
using Authentifications.Core.Interfaces;
using Authentifications.Application.Middlewares;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
namespace Authentifications.Tests;
public class UnitTestContextPathMiddleware
{
    [Fact]
    public async Task InvokeAsync_PathStartsWithContextPath_ShouldCallNextMiddleware()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/context/test";
        var nextMiddlewareMock = new Mock<RequestDelegate>();
        var middleware = new ContextPathMiddleware(nextMiddlewareMock.Object, "/context");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextMiddlewareMock.Verify(next => next(It.IsAny<HttpContext>()), Times.Once);
        Assert.Equal("/test", context.Request.Path);
    }
    [Fact]
    public async Task InvokeAsync_PathDoesNotStartWithContextPath_ShouldReturn404()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/other/test";
        var nextMiddlewareMock = new Mock<RequestDelegate>();
        var middleware = new ContextPathMiddleware(nextMiddlewareMock.Object, "/context");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextMiddlewareMock.Verify(next => next(It.IsAny<HttpContext>()), Times.Never);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

}