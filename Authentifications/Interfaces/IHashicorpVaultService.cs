namespace Authentifications.Interfaces;
public interface IHashicorpVaultService
{
    Task<string> GetRabbitConnectionStringFromVault();
    void StoreJwtPublicKeyInVault(string publicKeyPem);
}
