
using Authentifications.Core.Interfaces;
using Authentifications.Infrastructure.InternalServices;
using Moq;
using Xunit;

namespace Authentifications.Tests;
public class UnitTestRabbitMqService
{
    [Fact]
    public async Task EstablishConnection_ShouldReturnConnectionFactoryWithCorrectUri()
    {
        // Arrange
        var mockHashicorpVaultService = new Mock<IHashicorpVaultService>();
        mockHashicorpVaultService.Setup(service => service.GetRabbitConnectionStringFromVault())
            .ReturnsAsync("user:password@localhost:5672");

        var rabbitMqService = new RabbitMqService(mockHashicorpVaultService.Object);

        // Act
        var connectionFactory = await rabbitMqService.EstablishConnection();

        // Assert
        Assert.NotNull(connectionFactory);
        Assert.Equal(new Uri("amqp://user:password@localhost:5672"), connectionFactory.Uri);
    }
}