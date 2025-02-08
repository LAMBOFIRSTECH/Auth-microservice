using Authentifications.Interfaces;
using Hangfire;
namespace Authentifications.Services;
public class HangFireService : IHangFireService
{
    private readonly IRedisCacheService redisCacheService;
    private readonly ILogger<HangFireService> log;
    public HangFireService(IRedisCacheService redisCacheService, ILogger<HangFireService> log)
    {
        this.log = log;
        this.redisCacheService = redisCacheService;
    }
    public void ScheduleRetrieveDataFromExternalApi(bool result)
    {
        if (!result)
        {
            log.LogWarning("Le job Hangfire n'a pas été exécuté car result = false.");
            return;
        }
        try
        {
            log.LogInformation("Planification du job Hangfire : récupération des données externes.");
            string? jobId = null;
            const int retryCount = 3;
            for (int i = 0; i < retryCount; i++)
            {
                jobId = BackgroundJob.Schedule(
                    () => redisCacheService.RetrieveDataOnRedisUsingKeyAsync(),
                    TimeSpan.FromSeconds(40)
                );

                if (!string.IsNullOrEmpty(jobId))
                    break;
            }
            if (string.IsNullOrEmpty(jobId))
            {
                throw new Exception("La planification du job a échoué : jobId est null ou vide.");
            }

            log.LogInformation("Job Hangfire planifié avec succès. Job ID : {jobId}", jobId);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Erreur lors de la planification du job Hangfire.");
        }
    }
}
