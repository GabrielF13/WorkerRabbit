using MongoDB.Driver;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using WorkerRabbit.Models;
using WorkerRabbit.Services.Interfaces;

namespace WorkerRabbit
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IMongoCollection<NotificationEvent> _notificationLogs;
        private IConnection _connection;
        private IModel _channel;
        private bool _mongoEnabled;

        public Worker(
            ILogger<Worker> logger,
            IConfiguration configuration,
            IEmailService emailService,
            IMongoClient mongoClient = null)
        {
            _logger = logger;
            _configuration = configuration;
            _emailService = emailService;

            _mongoEnabled = mongoClient != null &&
                            !string.IsNullOrEmpty(_configuration["MongoDB:DatabaseName"]) &&
                            !string.IsNullOrEmpty(_configuration["MongoDB:CollectionName"]);

            if (_mongoEnabled && mongoClient != null)
            {
                var database = mongoClient.GetDatabase(_configuration["MongoDB:DatabaseName"]);
                _notificationLogs = database.GetCollection<NotificationEvent>(_configuration["MongoDB:CollectionName"]);
                _logger.LogInformation("MongoDB configurado com sucesso para logs de notifica��es");
            }
            else
            {
                _logger.LogWarning("MongoDB n�o configurado. Os logs de notifica��es n�o ser�o armazenados.");
            }

            InitializeRabbitMQ();
        }

        private void InitializeRabbitMQ()
        {
            try
            {
                _logger.LogInformation("Inicializando conex�o com RabbitMQ...");

                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
                    UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
                    Password = _configuration["RabbitMQ:Password"] ?? "guest",
                    VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/",
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.ExchangeDeclare(
                    exchange: "notification_exchange",
                    type: "topic",
                    durable: true,
                    autoDelete: false);

                _channel.QueueDeclare(
                    queue: "notification_queue",
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                _channel.QueueBind(
                    queue: "notification_queue",
                    exchange: "notification_exchange",
                    routingKey: "notifications");


                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                _logger.LogInformation("Conex�o com RabbitMQ estabelecida com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inicializar conex�o com RabbitMQ");
                throw;
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                NotificationEvent notification = null;

                try
                {
                    _logger.LogInformation($"Mensagem recebida: {ea.DeliveryTag}");

                    notification = JsonSerializer.Deserialize<NotificationEvent>(message);

                    if (notification == null)
                    {
                        _logger.LogError("Falha ao deserializar mensagem");
                        _channel.BasicReject(deliveryTag: ea.DeliveryTag, requeue: false);
                        return;
                    }

                    _logger.LogInformation($"Processando notifica��o: {notification.Id}, Tipo: {notification.Type}");

                    //var success = await _emailService.SendEmailAsync(notification);
                    var success = true;

                    notification.Sent = success;
                    notification.Timestamp = DateTime.UtcNow;

                    if (success)
                    {
                        if (_mongoEnabled && _notificationLogs != null)
                        {
                            await _notificationLogs.InsertOneAsync(notification);
                            _logger.LogInformation($"Log da notifica��o {notification.Id} armazenado no MongoDB");
                        }

                        _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        _logger.LogInformation($"Notifica��o {notification.Id} processada com sucesso");
                    }
                    else
                    {

                        _logger.LogError($"Notifica��o {notification.Id} falhou.");
                        if (_mongoEnabled && _notificationLogs != null)
                        {
                            notification.ErrorMessage = "Envio de notifica��o falhou";
                            await _notificationLogs.InsertOneAsync(notification);
                        }

                        _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                    }
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Erro ao deserializar mensagem de notifica��o");

                    _channel.BasicReject(deliveryTag: ea.DeliveryTag, requeue: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Erro ao processar mensagem de notifica��o: {ex.Message}");

                    if (notification != null && _mongoEnabled && _notificationLogs != null)
                    {
                        notification.ErrorMessage = ex.Message;
                        notification.Sent = false;
                        await _notificationLogs.InsertOneAsync(notification);
                    }

                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(
                queue: "notification_queue",
                autoAck: false, 
                consumer: consumer);

            _logger.LogInformation("Worker iniciado e aguardando mensagens...");

            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _channel?.Close();
            _connection?.Close();
            _logger.LogInformation("Worker de notifica��es parado");

            return base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();

            base.Dispose();
        }
    }
}