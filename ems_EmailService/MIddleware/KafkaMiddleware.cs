using Confluent.Kafka;
using Microsoft.Extensions.Options;
using ModalLayer;

namespace EmailRequest.MIddleware
{
    public class KafkaMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly KafkaServiceConfig _kafkaServiceConfig;
        private ILogger<KafkaMiddleware> _logger;

        public KafkaMiddleware(RequestDelegate next, IOptions<KafkaServiceConfig> options, ILogger<KafkaMiddleware> logger)
        {
            _next = next;
            _kafkaServiceConfig = options.Value;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {                
                var config = new ConsumerConfig
                {
                    GroupId = "gid-consumers",
                    BootstrapServers = $"{_kafkaServiceConfig.ServiceName}:{_kafkaServiceConfig.Port}"
                };

                _logger.LogInformation($"[Kafka] Start listning kafka topic: {_kafkaServiceConfig.AttendanceEmailTopic}");
                using (var consumer = new ConsumerBuilder<Null, string>(config).Build())
                {
                    consumer.Subscribe(_kafkaServiceConfig.AttendanceEmailTopic);
                    while (true)
                    {
                        _logger.LogInformation($"[Kafka] Waiting on topic: {_kafkaServiceConfig.AttendanceEmailTopic}");
                        var message = consumer.Consume();
                        _logger.LogInformation($"[Kafka] Message received: {message.Message.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"[Kafka] Error: {ex.Message}");
                throw;
            }
        }
    }
}
