using Confluent.Kafka;
using Microsoft.Extensions.Options;
using ModalLayer;

namespace EmailRequest.Service
{
    public class KafkaService : IHostedService
    {
        private readonly KafkaServiceConfig _kafkaServiceConfig;
        private ILogger<KafkaService> _logger;
        private IServiceProvider _serviceProvider;

        public KafkaService(IServiceProvider serviceProvider, ILogger<KafkaService> logger, IOptions<KafkaServiceConfig> options)
        {
            _serviceProvider = serviceProvider;
            _kafkaServiceConfig = options.Value;
            _logger = logger;
        }

        public KafkaServiceConfig KafkaServiceConfig => _kafkaServiceConfig;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[Kafka] Kafka listener registered successfully.");

            SubscribeKafkaTopic();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stoping service.");
            return Task.CompletedTask;
        }

        private void SubscribeKafkaTopic()
        {
            var config = new ConsumerConfig
            {
                GroupId = "gid-consumers",
                BootstrapServers = $"{KafkaServiceConfig.ServiceName}:{KafkaServiceConfig.Port}"
            };

            _logger.LogInformation($"[Kafka] Start listning kafka topic: {KafkaServiceConfig.AttendanceEmailTopic}");
            using (var consumer = new ConsumerBuilder<Null, string>(config).Build())
            {
                consumer.Subscribe(KafkaServiceConfig.AttendanceEmailTopic);
                while (true)
                {
                    _logger.LogInformation($"[Kafka] Waiting on topic: {KafkaServiceConfig.AttendanceEmailTopic}");
                    var message = consumer.Consume();
                    _logger.LogInformation($"[Kafka] Message received: {message.Message.Value}");
                }
            }
        }
    }
}
