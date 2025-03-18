using Authentifications.Application.Exceptions;
using Authentifications.Core.Interfaces;
using Hangfire;
namespace Authentifications.Infrastructure.InternalServices;
public class HangFireService : IHangFireService
{
    private readonly ILdapService ldapService;
    private readonly ILogger<HangFireService> log;
    public HangFireService(ILdapService ldapService, ILogger<HangFireService> log)
    {
        this.log = log;
        this.ldapService = ldapService;
    }
    public  string TryScheduleJob(Func<string> scheduleJobAction, int retryCount, TimeSpan delay)
    {
        string? jobId = null;
        for (int i = 0; i < retryCount; i++)
        {
            jobId = scheduleJobAction();
            if (!string.IsNullOrEmpty(jobId))
                break;

            Thread.Sleep(delay);
        }
        return jobId!;
    }
    [AutomaticRetry(Attempts = 1)]
    public void RetrieveDataFromOpenLdap(bool result, string message)
    {
        if (!result)
        {
            log.LogWarning("⚠ Le job Hangfire n'a pas été exécuté car result = false.");
            return;
        }
        try
        {
            log.LogInformation("🚀 Planification du job Hangfire pour récupération des données dans OpenLdap...");
            string jobId = TryScheduleJob(
                () => BackgroundJob.Schedule(
                    () => ldapService.RetrieveLdapData(message),
                    TimeSpan.FromSeconds(10)
                ),
                retryCount: 2,
                delay: TimeSpan.FromSeconds(1)
            );
            if (string.IsNullOrEmpty(jobId))
            {
                throw new HangFireException(500, "Error", "❌ La planification du job a échoué : jobId est null.");
            }
            log.LogInformation("✅ Job Hangfire planifié avec succès. Job ID : {JobId}", jobId);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "❌ Erreur lors de la planification du job Hangfire. Détails de l'exception");
        }
    }
}