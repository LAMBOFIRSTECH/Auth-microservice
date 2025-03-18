using RabbitMQ.Client;
namespace Authentifications.Core.Interfaces;
public interface IRabbitMqService
{
    Task<ConnectionFactory> EstablishConnection();
}