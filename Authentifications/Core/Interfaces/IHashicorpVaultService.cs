using Authentifications.Core.Entities;
namespace Authentifications.Core.Interfaces;
public interface IHashicorpVaultService
{
    Task<string> GetRabbitConnectionStringFromVault();
    Task<Message> StoreJwtPublicKeyInVault(string publicKeyPem);
    Task<string> GetLdapPassWordFromVault();
}