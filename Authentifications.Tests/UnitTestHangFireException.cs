using Authentifications.Application.Exceptions;
using Xunit;
namespace Authentifications.Tests;
public class UnitTestHangFireException
{
    [Fact]
    public void HangFireException_ShouldSetMessage()
    {
        // Arrange
        const int status = 500;
        const string type = "ErrorType";
        const string message = "An error occurred";

        // Act
        var exception = new HangFireException(status, type, message);

        // Assert
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void HangFireException_ShouldBeOfTypeException()
    {
        // Arrange
        var status = 500;
        var type = "ErrorType";
        var message = "An error occurred";

        // Act
        var exception = new HangFireException(status, type, message);

        // Assert
        Assert.IsAssignableFrom<Exception>(exception);
    }
}