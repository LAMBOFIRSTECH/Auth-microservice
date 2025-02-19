using Authentifications.Models;
namespace Authentifications.Interfaces;
public interface IRedisCacheService
{
	Task<ICollection<UtilisateurDto>> HangFireRetrieveDataOnRedisUsingKeyAsync();
	Task<ICollection<UtilisateurDto>> ValidateAndSyncRedisDataAsync(string cachedData);
	Task<(bool, UtilisateurDto)> GetBooleanAndUserDataFromRedisUsingParamsAsync(bool condition, string email, string password);
	Task<ICollection<UtilisateurDto>> RetrieveDataFromExternalApiAsync();
}
