using Authentifications.Core.Entities;
using Xunit;

namespace Authentifications.Tests;
public class UnitTestErrorMessageEntity
{
    [Fact]
    public void ErrorMessage_TypeProperty_ShouldGetAndSet()
    {
        // Arrange
        var errorMessage = new ErrorMessage();
        const string expectedType = "ErrorType";

        // Act
        errorMessage.Type = expectedType;

        // Assert
        Assert.Equal(expectedType, errorMessage.Type);
    }
    [Fact]
    public void ErrorMessage_TitleProperty_ShouldGetAndSet()
    {
        // Arrange
        var errorMessage = new ErrorMessage();
        const string expectedTitle = "ErrorTitle";

        // Act
        errorMessage.Title = expectedTitle;

        // Assert
        Assert.Equal(expectedTitle, errorMessage.Title);
    }

    [Fact]
    public void ErrorMessage_DetailProperty_ShouldGetAndSet()
    {
        // Arrange
        var errorMessage = new ErrorMessage();
        const string expectedDetail = "ErrorDetail";

        // Act
        errorMessage.Detail = expectedDetail;

        // Assert
        Assert.Equal(expectedDetail, errorMessage.Detail);
    }

    [Fact]
    public void ErrorMessage_StatusProperty_ShouldGetAndSet()
    {
        // Arrange
        var errorMessage = new ErrorMessage();
        const int expectedStatus = 404;

        // Act
        errorMessage.Status = expectedStatus;

        // Assert
        Assert.Equal(expectedStatus, errorMessage.Status);
    }

    [Fact]
    public void ErrorMessage_TraceIdProperty_ShouldGetAndSet()
    {
        // Arrange
        var errorMessage = new ErrorMessage();
        const string expectedTraceId = "TraceId123";

        // Act
        errorMessage.TraceId = expectedTraceId;

        // Assert
        Assert.Equal(expectedTraceId, errorMessage.TraceId);
    }

    [Fact]
    public void ErrorMessage_MessageProperty_ShouldGetAndSet()
    {
        // Arrange
        var errorMessage = new ErrorMessage();
        const string expectedMessage = "ErrorMessage";

        // Act
        errorMessage.Message = expectedMessage;

        // Assert
        Assert.Equal(expectedMessage, errorMessage.Message);
    }
}
