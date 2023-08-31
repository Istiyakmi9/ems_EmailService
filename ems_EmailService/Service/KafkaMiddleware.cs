﻿using Confluent.Kafka;
using EmailRequest.Service.TemplateService;
using Microsoft.Extensions.Options;
using ModalLayer;
using ModalLayer.Modal;
using ModalLayer.Modal.HtmlTemplateModel;
using Newtonsoft.Json;

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

            Task.Run(() =>
            {
                SubscribeKafkaTopic();
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stoping service.");
            return Task.CompletedTask;
        }

        private void SubscribeKafkaTopic()
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
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

                        HandleMessageSendEmail(message, scope);
                    }
                }
            }
        }

        private void HandleMessageSendEmail(ConsumeResult<Null, string> result, IServiceScope scope)
        {
            if (string.IsNullOrWhiteSpace(result.Message.Value))
                throw new Exception("[Kafka] Received invalid object from producer.");

            _logger.LogInformation($"[Kafka] Message received: {result.Message.Value}");
            var attendanceTemplate = scope.ServiceProvider.GetRequiredService<AttendanceTemplate>();
            AttendanceTemplateModel? attendanceTemplateModel = JsonConvert.DeserializeObject<AttendanceTemplateModel>(result.Message.Value);

            if (attendanceTemplateModel == null)
                throw new Exception("[Kafka] Received invalid object from producer.");

            _logger.LogInformation($"[Kafka] Starting sending request.");
            attendanceTemplate.SetupEmailTemplate(attendanceTemplateModel);
        }
    }
}