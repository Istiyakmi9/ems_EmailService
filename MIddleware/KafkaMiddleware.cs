using Confluent.Kafka;
using Microsoft.Extensions.Options;
using ModalLayer;

namespace EmailRequest.MIddleware
{
    public class KafkaMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly KafkaServiceConfig _kafkaServiceConfig;

        public KafkaMiddleware(RequestDelegate next, IOptions<KafkaServiceConfig> options)
        {
            _next = next;
            _kafkaServiceConfig = options.Value;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {                
                var config = new ConsumerConfig
                {
                    BootstrapServers = $"{_kafkaServiceConfig.ServiceName}:{_kafkaServiceConfig.Port}"
                };

                using (var consumer = new ConsumerBuilder<Null, string>(config).Build())
                {
                    consumer.Subscribe(_kafkaServiceConfig.AttendanceEmailTopic);
                    while (true)
                    {
                        var message = consumer.Consume();
                        Console.WriteLine(message.Message.Value);
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
