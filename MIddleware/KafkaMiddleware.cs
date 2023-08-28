using Confluent.Kafka;

namespace EmailRequest.MIddleware
{
    public class KafkaMiddleware
    {
        private readonly RequestDelegate _next;

        public KafkaMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                
                var config = new ConsumerConfig
                {
                    GroupId = "gid-consumers",
                    BootstrapServers = "localhost:9092"
                };
                using (var consumer = new ConsumerBuilder<Null, string>(config).Build())
                {
                    consumer.Subscribe("testdata");
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
