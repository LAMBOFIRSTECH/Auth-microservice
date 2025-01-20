namespace Authentifications.Interfaces;
public interface IRedisCacheTokenService
{
    void StoreRefreshTokenSessionInRedis(string email, string token, string password);
    Task<string> RetrieveTokenBasingOnRedisUserSessionAsync(string email, string password);
}
