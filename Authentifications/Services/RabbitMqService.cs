using System.Text;
using Authentifications.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
namespace Authentifications.Services;
public class RabbitMqService : IRabbitMqService
{
    private readonly ILogger<RabbitMqService> logger;
    private readonly IHashicorpVaultService hashicorpVaultService;
    private readonly IHangFireService hangFire;
    public RabbitMqService(ILogger<RabbitMqService> logger, IHashicorpVaultService hashicorpVaultService,IHangFireService hangFire)
    {
        this.logger = logger;
        this.hashicorpVaultService = hashicorpVaultService;
        this.hangFire= hangFire;
    }
    private async Task<ConnectionFactory> EstablishConnection()
    {
        var connectionString = await hashicorpVaultService.GetRabbitConnectionStringFromVault();
        var rabbitUri = new Uri("amqp://" + connectionString);
        return new ConnectionFactory { Uri = rabbitUri };
    }
    public async Task<string> RetrieveFromRabbitMq(string QueueName)
    {
        var factory = await EstablishConnection();
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        var result = string.Empty;
        var message = string.Empty;
        channel.QueueDeclare(queue: QueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
        var consumer = new EventingBasicConsumer(channel);
        var tcs = new TaskCompletionSource<string>();

        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            message = Encoding.UTF8.GetString(body); 
            logger.LogInformation("[x] Message reçu: {message}", message);
            if (!message.Contains("User created"))
            {
                tcs.TrySetCanceled();
                channel.BasicCancel(consumer.ConsumerTags.DefaultIfEmpty().FirstOrDefault() ?? string.Empty);
                return;
            }
            tcs.SetResult(message);  
            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        };
        channel.BasicConsume(queue: QueueName, autoAck: true, consumer: consumer);
        try
        {
            result = await tcs.Task;
            hangFire.ScheduleRetrieveDataFromExternalApi(true);
        }
        catch (TaskCanceledException)
        {
            logger.LogError("Le message ne correspondait pas aux critères.");
        }
        catch (Exception ex)
        {
            logger.LogError("Une erreur s'est produite: {ex.Message}", ex.Message);
        }
        return  result;
    }
}
