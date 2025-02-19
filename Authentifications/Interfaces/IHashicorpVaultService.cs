using Authentifications.Models;
namespace Authentifications.Interfaces;
public interface IHashicorpVaultService
{
    Task<string> GetRabbitConnectionStringFromVault();
    Task<Message> StoreJwtPublicKeyInVault(string publicKeyPem);
}
