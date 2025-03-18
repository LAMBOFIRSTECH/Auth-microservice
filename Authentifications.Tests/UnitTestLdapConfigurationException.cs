using Authentifications.Application.Exceptions;
using Xunit;
namespace Authentifications.Tests;
public class UnitTestLdapConfigurationException
{

    [Fact]
    public void Constructor_ShouldInitializeException_WithGivenParameters()
    {
        // Arrange
        int status = 500;
        string type = "ConfigurationError";
        string message = "Invalid LDAP configuration.";

        // Act
        var exception = new LdapConfigurationException(status, type, message);

        // Assert
        Assert.Equal(message, exception.Message);
    }
}