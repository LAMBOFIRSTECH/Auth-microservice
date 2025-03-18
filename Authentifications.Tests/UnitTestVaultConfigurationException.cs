using Authentifications.Application.Exceptions;
using Xunit;
namespace Authentifications.Tests;
public class UnitTestVaultConfigurationException
{
    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange
        int status = 404;
        string type = "NotFound";
        string message = "Vault configuration not found";

        // Act
        var exception = new VaultConfigurationException(status, type, message);

        // Assert
        Assert.Equal(message, exception.Message);
    }
}