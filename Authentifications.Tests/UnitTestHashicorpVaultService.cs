using Authentifications.Application.Exceptions;
using Authentifications.Infrastructure.InternalServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Authentifications.Tests;
public class UnitTestHashicorpVaultService
{
    private readonly Mock<IConfiguration> mockConfiguration;
    private readonly Mock<ILogger<HashicorpVaultService>> mockLogger;
    private readonly HashicorpVaultService vaultService;

    public UnitTestHashicorpVaultService()
    {
        mockConfiguration = new Mock<IConfiguration>();
        mockLogger = new Mock<ILogger<HashicorpVaultService>>();
        vaultService = new HashicorpVaultService(mockConfiguration.Object, mockLogger.Object);
    }
    [Fact]
    public async Task GetAppRoleTokenFromVault_ShouldThrowException_WhenConfigurationsAreInvalid()
    {
        // Arrange
        mockConfiguration.Setup(c => c["HashiCorp:AppRole:RoleID"]).Returns(string.Empty);
        mockConfiguration.Setup(c => c["HashiCorp:AppRole:SecretID"]).Returns(string.Empty);
        mockConfiguration.Setup(c => c["HashiCorp:HttpClient:BaseAddress"]).Returns(string.Empty);

        // Act & Assert
        await Assert.ThrowsAsync<VaultConfigurationException>(() => vaultService.GetAppRoleTokenFromVault());
    }

}