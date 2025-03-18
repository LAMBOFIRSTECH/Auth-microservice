using Authentifications.Core.Interfaces;
using RabbitMQ.Client;
namespace Authentifications.Infrastructure.InternalServices;
public class RabbitMqService : IRabbitMqService
{
    private readonly IHashicorpVaultService hashicorpVaultService;
    public RabbitMqService(IHashicorpVaultService hashicorpVaultService)
    {
        this.hashicorpVaultService = hashicorpVaultService;
    }
    public async Task<ConnectionFactory> EstablishConnection()
    {
        var connectionString = await hashicorpVaultService.GetRabbitConnectionStringFromVault();
        var rabbitUri = new Uri("amqp://" + connectionString);
        return new ConnectionFactory { Uri = rabbitUri };
    }
}