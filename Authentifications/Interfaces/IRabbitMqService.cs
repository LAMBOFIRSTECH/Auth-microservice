using RabbitMQ.Client;
namespace Authentifications.Interfaces;
public interface IRabbitMqService
{
    Task<ConnectionFactory> EstablishConnection();
}
