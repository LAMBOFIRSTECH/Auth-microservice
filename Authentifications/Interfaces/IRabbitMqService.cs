namespace Authentifications.Interfaces;
public interface IRabbitMqService
{
    Task<string> RetrieveFromRabbitMq(string QueueName);
}