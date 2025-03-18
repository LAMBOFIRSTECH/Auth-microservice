using Authentifications.Application.Middlewares;
using Xunit;
namespace Authentifications.Tests;
public class UnitTestAuthentificationBasicException
{
    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Arrange
        var message = "Test message";

        // Act
        var exception = new AuthentificationBasicException(message);

        // Assert
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void Constructor_WithoutParameters_ShouldSetDefaultMessage()
    {
        // Act
        var exception = new AuthentificationBasicException();

        // Assert
        Assert.NotNull(exception.Message);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetMessageAndInnerException()
    {
        // Arrange
        var message = "Test message";
        var innerException = new Exception("Inner exception");

        // Act
        var exception = new AuthentificationBasicException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }
}