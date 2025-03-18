namespace Authentifications.Core.Interfaces;
public interface IRedisCacheTokenService
{
    void StoreRefreshTokenSessionInRedis(string email, string refreshToken, string password);
    Task<string> RetrieveTokenBasingOnRedisUserSessionAsync(string email, string password);
}