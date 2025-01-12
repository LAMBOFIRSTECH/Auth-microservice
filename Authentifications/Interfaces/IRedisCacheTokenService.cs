using Authentifications.Models;

namespace Authentifications.Interfaces;
public interface IRedisCacheTokenService
{
    byte[] ComputeHashUsingByte(string email, string password);
    void StoreRefreshTokenSessionInRedis(string email, string token, string password);
    Task<string> RetrieveTokenBasingOnRedisUserSessionAsync(string email, string password);
}
