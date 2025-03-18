using Authentifications.Application.DTOs;
namespace Authentifications.Core.Interfaces;
public interface IRedisCacheService
{
	Task<(bool, UtilisateurDto)> GetBooleanAndUserDataFromRedisUsingParamsAsync(bool condition, string email, string password);
	Task<ICollection<UtilisateurDto>> GetUserUsingUuidAsync(ICollection<UtilisateurDto> utilisateurs);
}