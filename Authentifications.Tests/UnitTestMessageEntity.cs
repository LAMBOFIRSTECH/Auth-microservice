using Authentifications.Core.Entities;
using Xunit;

namespace Authentifications.Tests;
public class UnitTestMessageEntity
{
    [Fact]
    public void Message_Type_ShouldBeSetAndRetrieved()
    {
        // Arrange
        var message = new Message();
        const string expectedType = "Error";

        // Act
        message.Type = expectedType;

        // Assert
        Assert.Equal(expectedType, message.Type);
    }
    [Fact]
    public void Message_Title_ShouldBeSetAndRetrieved()
    {
        // Arrange
        var message = new Message();
        const string expectedTitle = "Test Title";

        // Act
        message.Title = expectedTitle;

        // Assert
        Assert.Equal(expectedTitle, message.Title);
    }
    [Fact]
    public void Message_Detail_ShouldBeSetAndRetrieved()
    {
        // Arrange
        var message = new Message();
        const string expectedDetail = "This is a test detail.";

        // Act
        message.Detail = expectedDetail;

        // Assert
        Assert.Equal(expectedDetail, message.Detail);
    }
    [Fact]
    public void Message_Status_ShouldBeSetAndRetrieved()
    {
        // Arrange
        var message = new Message();
        const int expectedStatus = 200;

        // Act
        message.Status = expectedStatus;

        // Assert
        Assert.Equal(expectedStatus, message.Status);
    }
}