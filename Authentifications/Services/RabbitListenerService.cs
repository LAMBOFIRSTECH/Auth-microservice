using System.Text;
using Authentifications.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
namespace Authentifications.Services;
public class RabbitListenerService : BackgroundService
{
    private readonly ILogger<RabbitListenerService> log;
    private readonly IRabbitMqService rabbitMqService;
    private readonly IHangFireService hangFire;
    private IConnection? connection;
    private IModel? channel;

    public RabbitListenerService(ILogger<RabbitListenerService> log, IRabbitMqService rabbitMqService, IHangFireService hangFire)
    {
        this.log = log;
        this.rabbitMqService = rabbitMqService;
        this.hangFire = hangFire;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Run(() => StartListening("authentification"), stoppingToken);
    }
    private async void StartListening(string queueName)
    {
        try
        {
            var factory = await rabbitMqService.EstablishConnection();
            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                log.LogInformation("[x] Message reçu: {message}", message);
                try
                {
                    if (message.Contains("User created"))
                    {
                        hangFire.ScheduleRetrieveDataFromExternalApi(true);
                    }
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Erreur lors du traitement du message");
                    channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };
            channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
            log.LogInformation("Listening to messages on queue: {QueueName}", queueName);
            while (!connection.IsOpen)
            {
                Thread.Sleep(1000);
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Erreur pendant l'écoute des messages");
        }
    }
    public override void Dispose()
    {
        channel?.Close();
        connection?.Close();
        base.Dispose();
    }
}
