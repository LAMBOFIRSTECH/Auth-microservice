using Authentifications.Core.Interfaces;
using Authentifications.Infrastructure.InternalServices;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Authentifications.Tests;
public class UnitTestHangFireService
{
    private readonly Mock<ILdapService> ldapServiceMock;
    private readonly Mock<ILogger<HangFireService>> loggerMock;
    private readonly HangFireService hangFireService;

    public UnitTestHangFireService()
    {
        ldapServiceMock = new Mock<ILdapService>();
        loggerMock = new Mock<ILogger<HangFireService>>();
        hangFireService = new HangFireService(ldapServiceMock.Object, loggerMock.Object);
    }
    [Fact]
    public void TryScheduleJob_ShouldReturnJobId_WhenJobIsScheduledSuccessfully()
    {
        // Arrange
        Func<string> scheduleJobAction = () => "jobId";
        const int retryCount = 2;
        TimeSpan delay = TimeSpan.FromSeconds(1);

        // Act
        string result = hangFireService.TryScheduleJob(scheduleJobAction, retryCount, delay);

        // Assert
        Assert.Equal("jobId", result);
    }

    [Fact]
    public void TryScheduleJob_ShouldReturnNull_WhenJobIsNotScheduled()
    {
        // Arrange
        Func<string> scheduleJobAction = () => null;
        int retryCount = 2;
        TimeSpan delay = TimeSpan.FromSeconds(1);

        // Act
        string result = hangFireService.TryScheduleJob(scheduleJobAction, retryCount, delay);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void RetrieveDataFromOpenLdap_ShouldLogWarning_WhenResultIsFalse()
    {
        // Arrange
        bool result = false;
        string message = "test message";

        // Act
        hangFireService.RetrieveDataFromOpenLdap(result, message);

        // Assert
        loggerMock.Verify(
            log => log.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("⚠ Le job Hangfire n'a pas été exécuté car result = false.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
    [Fact]
    public void RetrieveDataFromOpenLdap_ShouldLogError_WhenExceptionIsThrown()
    {
        // Arrange
        bool result = true;
        string message = "test message";
        ldapServiceMock.Setup(x => x.RetrieveLdapData(It.IsAny<string>())).Throws(new Exception("Test exception"));

        // Act
        hangFireService.RetrieveDataFromOpenLdap(result, message);

        // Assert
        loggerMock.Verify(
            log => log.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("❌ Erreur lors de la planification du job Hangfire. Détails de l'exception")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

}