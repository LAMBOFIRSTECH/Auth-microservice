using Authentifications.Application.Middlewares;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using System.Security.Authentication;
namespace Authentifications.Tests;
public class UnitTestValidationHandlingMiddleware
{
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly ValidationHandlingMiddleware _middleware;
    private readonly DefaultHttpContext _context;

    public UnitTestValidationHandlingMiddleware()
    {
        _nextMock = new Mock<RequestDelegate>();
        _middleware = new ValidationHandlingMiddleware(_nextMock.Object);
        _context = new DefaultHttpContext();
    }
    [Fact]
    public async Task InvokeAsync_ShouldHandleModelValidationErrors()
    {
        // Arrange
        _context.Items["ModelValidationErrors"] = new List<string> { "Error1", "Error2" };

        // Act
        await _middleware.InvokeAsync(_context);

        // Assert
        Assert.Equal(400, _context.Response.StatusCode);
        Assert.StartsWith("application/json", _context.Response.ContentType);
    }
    [Fact]
    public async Task InvokeAsync_ShouldHandleAuthenticationException()
    {
        // Arrange
        _nextMock.Setup(next => next(It.IsAny<HttpContext>())).Throws(new AuthenticationException());

        // Act
        await _middleware.InvokeAsync(_context);

        // Assert
        Assert.Equal(401, _context.Response.StatusCode);
        Assert.StartsWith("application/json", _context.Response.ContentType);
    }
    [Fact]
    public async Task InvokeAsync_ShouldHandleArgumentException()
    {
        // Arrange
        _nextMock.Setup(next => next(It.IsAny<HttpContext>())).Throws(new ArgumentException());

        // Act
        await _middleware.InvokeAsync(_context);

        // Assert
        Assert.Equal(400, _context.Response.StatusCode);
        Assert.StartsWith("application/json", _context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleUnexpectedException()
    {
        // Arrange
        _nextMock.Setup(next => next(It.IsAny<HttpContext>())).Throws(new Exception());

        // Act
        await _middleware.InvokeAsync(_context);

        // Assert
        Assert.Equal(500, _context.Response.StatusCode);
        Assert.StartsWith("application/json", _context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleKeyNotFoundException()
    {
        // Arrange
        _nextMock.Setup(next => next(It.IsAny<HttpContext>())).Throws(new KeyNotFoundException());

        // Act
        await _middleware.InvokeAsync(_context);

        // Assert
        Assert.Equal(404, _context.Response.StatusCode);
        Assert.StartsWith("application/json", _context.Response.ContentType);
    }
}